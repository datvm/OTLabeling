namespace VerseLabeling.Services;

sealed class IsraeliteLabeler(LabelingContext database, ConfigProvider config, IChatClient chat, string apiKey)
{
    public async Task RunAsync(RunSettings settings)
    {
        LabelRun? run = null;
        SelectedChapter? current = null;
        try
        {
            var chapters = await SelectChaptersAsync(settings);
            run = await InsertRunAsync(settings, chapters);
            var log = new RunLog(Path.Combine(
                config.AppConfigPath,
                "out",
                "verse-labeling",
                run.Step,
                run.RunKey));
            log.WriteSettings(run, settings.RetryDelaySeconds);

            var gap = TimeSpan.FromSeconds(60d / settings.Rpm);
            long? requestStart = null;

            async Task WaitForTurnAsync()
            {
                if (requestStart is long started)
                {
                    var remain = gap - Stopwatch.GetElapsedTime(started);
                    if (remain > TimeSpan.Zero)
                        await Task.Delay(remain);
                }

                requestStart = Stopwatch.GetTimestamp();
            }
            string? bookCode = null;
            string? carriedSummary = null;
            string? carriedImmediate = null;

            foreach (var chapter in chapters)
            {
                current = chapter;
                Console.WriteLine($"{chapter.Book.Code} {chapter.Chapter} started.");
                var (summary, immediate) = ContextFor(chapter, ref bookCode, ref carriedSummary, ref carriedImmediate, settings);
                var request = ChapterPrompt.Build(summary, immediate, chapter.Verses);
                var messages = new List<ChatMessage> { new(ChatRole.User, request) };
                await WaitForTurnAsync();
                var body = await SendAsync(messages, settings.RetryDelaySeconds);
                var missingTagRetries = 0;
                ChapterOutput output;
                while (true)
                {
                    log.WriteRaw(chapter.Book.Code, chapter.Chapter, body, missingTagRetries == 0 ? null : missingTagRetries + 1);
                    output = ChapterCheck.Parse(body);
                    ChapterCheck.DistinctTags(output);
                    var errors = ChapterCheck.Check(
                        output,
                        chapter.Book.Code,
                        chapter.Chapter,
                        chapter.Verses);
                    if (errors.Count == 0)
                        break;

                    var joined = string.Join(' ', errors);
                    if (!CanRetryMissingTag(errors))
                        throw new LabelingException("The check failed: " + joined);

                    if (missingTagRetries == MaxMissingTagRetries)
                    {
                        Console.WriteLine($"The check retry limit of {MaxMissingTagRetries} was reached.");
                        throw new LabelingException("The check failed: " + joined);
                    }

                    if (missingTagRetries == 0)
                        log.WriteRaw(chapter.Book.Code, chapter.Chapter, body, 1);

                    missingTagRetries++;
                    Console.WriteLine($"{chapter.Book.Code} {chapter.Chapter} check retry {missingTagRetries} of {MaxMissingTagRetries}. A verse tag is not in Tags.");
                    messages.Add(new ChatMessage(ChatRole.Assistant, body));
                    messages.Add(new ChatMessage(ChatRole.User, MissingTagRetryMessage(request, body, errors)));
                    await WaitForTurnAsync();
                    body = await SendAsync(messages, settings.RetryDelaySeconds);
                }

                await SaveChapterAsync(run, chapter, output);
                var byVerse = output.Verses!.ToDictionary(verse => verse.VerseNumber);
                log.AppendChapter(
                    chapter.Book.Code,
                    chapter.Chapter,
                    chapter.Verses,
                    byVerse,
                    output.SummarySoFar!,
                    output.ImmediateContext!);
                carriedSummary = output.SummarySoFar;
                carriedImmediate = output.ImmediateContext;
                Console.WriteLine($"{chapter.Book.Code} {chapter.Chapter} done.");
            }

            current = null;
            run.Status = LabelRun.StatusCompleted;
            await database.SaveChangesAsync();
            Console.WriteLine("Run completed.");
        }
        catch (Exception ex)
        {
            if (current is not null)
                Console.WriteLine($"{current.Book.Code} {current.Chapter} failed.");

            await MarkFailedAsync(run);
            if (ex is LabelingException)
                throw;

            throw new LabelingException(Redact(ex.Message));
        }
    }

    static readonly JsonSerializerOptions ResponseSchema = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    const int MaxMissingTagRetries = 3;
    const int MaxContinuousRetries = int.MaxValue;
    static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMilliseconds(int.MaxValue);

    int retryStreak;

    static readonly string[] RetryableVerseErrors = [" is not in Tags."];

    static bool CanRetryMissingTag(IReadOnlyList<string> errors) =>
        errors.All(error =>
            error.StartsWith("Verse ", StringComparison.Ordinal) &&
            RetryableVerseErrors.Any(suffix => error.EndsWith(suffix, StringComparison.Ordinal)));

    static string MissingTagRetryMessage(string originalInput, string response, IReadOnlyList<string> errors) =>
        string.Join('\n',
        [
            "The check failed: " + string.Join(' ', errors),
            "",
            "A verse tag is missing from the Tags array.",
            "Return a full JSON response for the same chapter.",
            "Put every verse tag into Tags, including its parents.",
            "Do not return only the missing tags.",
            "",
            "Original input:",
            originalInput,
            "",
            "Your response:",
            response,
        ]);

    async Task<string> SendAsync(IReadOnlyList<ChatMessage> messages, int retryDelaySeconds)
    {
        while (true)
        {
            try
            {
                var response = await chat.GetResponseAsync<ChapterOutput>(messages, ResponseSchema);
                retryStreak = 0;
                if (string.IsNullOrEmpty(response.Text))
                    throw new LabelingException("The API call returned no body.");

                return response.Text;
            }
            catch (LabelingException)
            {
                throw;
            }
            catch (Exception ex) when (RetryStatus(ex) is int status)
            {
                if (retryStreak == MaxContinuousRetries)
                {
                    Console.WriteLine($"The API returned {status}. The retry limit of {MaxContinuousRetries} was reached.");
                    throw new LabelingException("The API call failed: " + Redact(ex.Message));
                }

                retryStreak++;
                var wait = RetryDelay(retryDelaySeconds, retryStreak);
                var waitSeconds = (long)wait.TotalSeconds;
                Console.WriteLine($"The API returned {status}. Retry {retryStreak}. The next attempt waits {waitSeconds} seconds.");
                await Task.Delay(wait);
            }
            catch (Exception ex)
            {
                throw new LabelingException("The API call failed: " + Redact(ex.Message));
            }
        }
    }

    static TimeSpan RetryDelay(int baseSeconds, int streak)
    {
        var doublings = (streak - 1) / 3;
        long seconds = baseSeconds;
        for (var i = 0; i < doublings && seconds <= long.MaxValue / 2; i++)
            seconds *= 2;

        var delay = TimeSpan.FromSeconds(seconds);
        return delay <= MaxRetryDelay ? delay : MaxRetryDelay;
    }

    static int? RetryStatus(Exception exception)
    {
        var seen = new HashSet<Exception>();
        return Find(exception);

        int? Find(Exception? current)
        {
            if (current is null || !seen.Add(current))
                return null;

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    var aggregateStatus = Find(inner);
                    if (aggregateStatus is not null)
                        return aggregateStatus;
                }
            }

            if (StatusCode(current) is int code and (429 or 503))
                return code;

            if (MessageStatus(current.Message) is int messageStatus)
                return messageStatus;

            return Find(current.InnerException);
        }
    }

    static int? StatusCode(Exception exception)
    {
        if (exception is System.ClientModel.ClientResultException client)
            return client.Status;

        if (exception is HttpRequestException http && http.StatusCode is System.Net.HttpStatusCode httpCode)
            return (int)httpCode;

        foreach (var property in exception.GetType().GetProperties())
        {
            if (property.Name is not ("Status" or "StatusCode"))
                continue;

            var value = property.GetValue(exception);
            if (value is int number)
                return number;

            if (value is System.Net.HttpStatusCode status)
                return (int)status;
        }

        return null;
    }

    static int? MessageStatus(string message)
    {
        if (message.Contains("Service Unavailable", StringComparison.OrdinalIgnoreCase))
            return 503;

        if (message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
            return 429;

        if (message.Contains("HTTP 503", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Status: 503", StringComparison.OrdinalIgnoreCase))
            return 503;

        if (message.Contains("HTTP 429", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Status: 429", StringComparison.OrdinalIgnoreCase))
            return 429;

        return null;
    }

    async Task<IReadOnlyList<SelectedChapter>> SelectChaptersAsync(RunSettings settings)
    {
        var storedBooks = await database.BibleBooks.ToListAsync();
        var byCode = storedBooks.ToDictionary(book => book.Code, StringComparer.OrdinalIgnoreCase);
        var books = new List<BibleBook>(settings.BookCodes.Count);
        foreach (var code in settings.BookCodes)
        {
            if (!byCode.TryGetValue(code, out var book))
                throw new LabelingException($"Book code '{code}' is not a book code.");

            books.Add(book);
        }

        var selected = new List<SelectedChapter>();
        foreach (var book in books)
        {
            var stored = await database.BibleVerses
                .Where(verse => verse.BookId == book.Id)
                .Select(verse => verse.Chapter)
                .Distinct()
                .ToListAsync();
            if (stored.Count == 0)
                throw new LabelingException($"{book.Code} has no verses.");

            var numbers = ChapterNumbers(book.Code, stored, settings.Range);
            var verses = await database.BibleVerses
                .Where(verse => verse.BookId == book.Id && numbers.Contains(verse.Chapter))
                .OrderBy(verse => verse.Chapter)
                .ThenBy(verse => verse.VerseNumber)
                .ToListAsync();
            var byChapter = verses
                .GroupBy(verse => verse.Chapter)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<BibleVerse>)group.ToList());

            foreach (var number in numbers)
                selected.Add(new SelectedChapter(book, number, byChapter[number]));
        }

        return selected;
    }

    static List<int> ChapterNumbers(string bookCode, List<int> stored, ChapterRange? range)
    {
        if (range is null)
            return stored.Order().ToList();

        var present = stored.ToHashSet();
        var last = stored.Max();
        var end = range.End ?? last;
        if (range.Start > end)
            throw new LabelingException($"Chapter {range.Start} is not in {bookCode}.");

        var numbers = new List<int>();
        for (var number = range.Start; number <= end; number++)
        {
            if (!present.Contains(number))
                throw new LabelingException($"Chapter {number} is not in {bookCode}.");

            numbers.Add(number);
        }

        return numbers;
    }

    async Task<LabelRun> InsertRunAsync(RunSettings settings, IReadOnlyList<SelectedChapter> chapters)
    {
        var run = new LabelRun
        {
            RunKey = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture),
            Step = settings.Step,
            Books = string.Join(',', chapters.Select(chapter => chapter.Book.Code).Distinct()),
            Chapters = settings.ChaptersText,
            Summary = settings.Summary,
            ImmediateContext = settings.ImmediateContext,
            Rpm = settings.Rpm,
            BaseUrl = settings.BaseUrl,
            Model = settings.Model,
            Status = LabelRun.StatusRunning,
        };

        database.LabelRuns.Add(run);
        try
        {
            await database.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            database.Entry(run).State = EntityState.Detached;
            run.Id = 0;
            var detail = ex.InnerException?.Message ?? ex.Message;
            if (detail.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                throw new LabelingException("A run with this RunKey already exists.");

            throw new LabelingException("The database write failed: " + detail);
        }

        return run;
    }

    async Task<Dictionary<int, HashSet<string>>> RootNamesAsync(IReadOnlyCollection<VerseTag> seeds)
    {
        var tags = seeds.ToDictionary(tag => tag.Id);
        var links = new List<VerseTagParent>();
        var pending = seeds.Select(tag => tag.Id).ToHashSet();
        while (pending.Count > 0)
        {
            var batch = pending.ToArray();
            pending.Clear();
            var found = await database.VerseTagParents
                .Where(link => batch.Contains(link.TagId))
                .ToListAsync();
            links.AddRange(found);
            var missing = found
                .Select(link => link.ParentTagId)
                .Distinct()
                .Where(id => !tags.ContainsKey(id))
                .ToArray();
            if (missing.Length == 0)
                continue;

            var parents = await database.VerseTags
                .Where(tag => missing.Contains(tag.Id))
                .ToListAsync();
            foreach (var parent in parents)
                tags[parent.Id] = parent;

            foreach (var id in missing)
                pending.Add(id);
        }

        var byChild = links
            .GroupBy(link => link.TagId)
            .ToDictionary(group => group.Key, group => group.Select(link => link.ParentTagId).ToList());
        var cache = new Dictionary<int, HashSet<string>>();

        HashSet<string> Roots(int id, HashSet<int> stack)
        {
            if (cache.TryGetValue(id, out var found))
                return found;

            if (!stack.Add(id))
                return [];

            var names = new HashSet<string>(StringComparer.Ordinal);
            if (!byChild.TryGetValue(id, out var parents) || parents.Count == 0)
            {
                if (tags.TryGetValue(id, out var tag) && ChapterCheck.IsRoot(tag.Name))
                    names.Add(tag.Name);
            }
            else
            {
                foreach (var parent in parents)
                    names.UnionWith(Roots(parent, stack));
            }

            stack.Remove(id);
            cache[id] = names;
            return names;
        }

        var result = new Dictionary<int, HashSet<string>>();
        foreach (var seed in seeds)
            result[seed.Id] = Roots(seed.Id, []);

        return result;
    }

    async Task SaveChapterAsync(LabelRun run, SelectedChapter chapter, ChapterOutput output)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        try
        {
            var responseVerses = output.Verses ?? throw new LabelingException("Verses is absent.");
            var byVerse = responseVerses.ToDictionary(verse => verse.VerseNumber);
            var catalog = TagCatalog.Create(output.Tags);
            var names = catalog.Tags.Select(tag => tag.Name!).Distinct(StringComparer.Ordinal).ToArray();
            var existing = await database.VerseTags
                .Where(tag => names.Contains(tag.Name))
                .ToListAsync();
            var rootsById = await RootNamesAsync(existing);
            var byKey = existing.ToDictionary(tag => (tag.Name, tag.Subclassifier));
            var bySource = new Dictionary<TagOutput, VerseTag>();
            foreach (var tag in catalog.Tags)
            {
                var key = (Name: tag.Name!, Subclassifier: catalog.StoredSubclassifier(tag));
                if (!byKey.TryGetValue(key, out var row))
                {
                    row = ReuseBare(key.Name, key.Subclassifier);
                    if (row is null)
                    {
                        row = new VerseTag { Name = key.Name, Subclassifier = key.Subclassifier };
                        database.VerseTags.Add(row);
                        byKey.Add(key, row);
                    }
                }

                bySource[tag] = row;
            }

            await database.SaveChangesAsync();

            var touchedIds = bySource.Values.Select(tag => tag.Id).Distinct().ToArray();
            if (touchedIds.Length > 0)
            {
                await database.VerseTagParents
                    .Where(link => touchedIds.Contains(link.TagId))
                    .ExecuteDeleteAsync();
            }

            VerseTag RequireTag(string text, IReadOnlyList<string?> context)
            {
                if (!catalog.TryResolve(text, context, out var match, out var error))
                    throw new LabelingException($"Tag '{text}' {error}.");

                return bySource[match!];
            }

            VerseTag? ReuseBare(string name, string subclassifier)
            {
                if (subclassifier is not ("People" or "Places"))
                    return null;

                if (!byKey.TryGetValue((name, ""), out var bare))
                    return null;

                return rootsById.TryGetValue(bare.Id, out var roots) && roots.Count == 1 && roots.Contains(subclassifier)
                    ? bare
                    : null;
            }

            foreach (var item in catalog.Tags)
            {
                var tagId = bySource[item].Id;
                foreach (var parentName in item.Parents ?? [])
                {
                    if (string.IsNullOrWhiteSpace(parentName))
                        continue;

                    database.VerseTagParents.Add(new VerseTagParent
                    {
                        TagId = tagId,
                        ParentTagId = RequireTag(parentName, item.Parents!).Id,
                    });
                }
            }

            var verseIds = chapter.Verses.Select(verse => verse.Id).ToArray();
            var resolved = await database.VerseTagResolutions
                .Where(resolution => verseIds.Contains(resolution.VerseId))
                .Select(resolution => resolution.VerseId)
                .ToListAsync();
            var resolvedIds = resolved.ToHashSet();
            var replaceIds = verseIds.Where(id => !resolvedIds.Contains(id)).ToArray();
            if (replaceIds.Length > 0)
            {
                await database.VerseTagMappings
                    .Where(mapping => replaceIds.Contains(mapping.VerseId))
                    .ExecuteDeleteAsync();
            }

            foreach (var verse in chapter.Verses)
            {
                var tags = byVerse[verse.VerseNumber].Tags!;
                for (var index = 0; index < tags.Count; index++)
                {
                    var tag = RequireTag(tags[index]!, tags);
                    database.LabelRunTags.Add(new LabelRunTag
                    {
                        LabelRunId = run.Id,
                        VerseId = verse.Id,
                        VerseTagId = tag.Id,
                        TagIndex = index + 1,
                    });

                    if (resolvedIds.Contains(verse.Id))
                        continue;

                    database.VerseTagMappings.Add(new VerseTagMapping
                    {
                        VerseId = verse.Id,
                        VerseTagId = tag.Id,
                        TagIndex = index + 1,
                    });
                }
            }

            await database.SaveChangesAsync();
            await transaction.CommitAsync();
            DetachExceptRun();
        }
        catch
        {
            DetachExceptRun();
            throw;
        }
    }

    static (string Summary, string Immediate) ContextFor(
        SelectedChapter chapter,
        ref string? bookCode,
        ref string? carriedSummary,
        ref string? carriedImmediate,
        RunSettings settings)
    {
        if (chapter.Book.Code == bookCode)
            return (carriedSummary!, carriedImmediate!);

        var laterBook = bookCode is not null;
        bookCode = chapter.Book.Code;
        if (laterBook)
            return ("None", "None");

        return (settings.Summary, settings.ImmediateContext);
    }

    async Task MarkFailedAsync(LabelRun? run)
    {
        if (run is not { Id: > 0 })
            return;

        try
        {
            DetachExceptRun();
            if (database.Entry(run).State == EntityState.Detached)
                database.Attach(run);

            var entry = database.Entry(run);
            entry.Property(item => item.Status).CurrentValue = LabelRun.StatusFailed;
            entry.Property(item => item.Status).IsModified = true;
            await database.SaveChangesAsync();
        }
        catch
        {
            // The original error still exits the process.
        }
    }

    void DetachExceptRun()
    {
        foreach (var entry in database.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is LabelRun)
                continue;

            entry.State = EntityState.Detached;
        }
    }

    string Redact(string message) =>
        message.Replace(apiKey, "[api key]", StringComparison.Ordinal);
}

sealed record SelectedChapter(BibleBook Book, int Chapter, IReadOnlyList<BibleVerse> Verses);
