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
                var checkRetries = 0;
                ChapterOutput output;
                while (true)
                {
                    log.WriteRaw(chapter.Book.Code, chapter.Chapter, body, checkRetries == 0 ? null : checkRetries + 1);
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
                    if (!CanRetryCheck(errors))
                        throw new LabelingException("The check failed: " + joined);

                    if (checkRetries == MaxCheckRetries)
                    {
                        Console.WriteLine($"The check retry limit of {MaxCheckRetries} was reached.");
                        throw new LabelingException("The check failed: " + joined);
                    }

                    if (checkRetries == 0)
                        log.WriteRaw(chapter.Book.Code, chapter.Chapter, body, 1);

                    checkRetries++;
                    Console.WriteLine($"{chapter.Book.Code} {chapter.Chapter} check retry {checkRetries} of {MaxCheckRetries}. The check failed: {joined}");
                    messages.Add(new ChatMessage(ChatRole.Assistant, body));
                    messages.Add(new ChatMessage(ChatRole.User, CheckRetryMessage(request, body, errors)));
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

            throw new LabelingException(Redact(FullMessage(ex)));
        }
    }

    static readonly JsonSerializerOptions ResponseSchema = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    const int MaxCheckRetries = 3;
    const int MaxContinuousRetries = int.MaxValue;
    static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMilliseconds(int.MaxValue);

    int retryStreak;

    static bool CanRetryCheck(IReadOnlyList<string> errors) =>
        errors.Count > 0 && errors.All(IsRetryableCheck);

    static bool IsRetryableCheck(string error)
    {
        if (error.StartsWith("Verse ", StringComparison.Ordinal))
        {
            return error.Contains(" tag '", StringComparison.Ordinal)
                || error.EndsWith(" has a blank tag.", StringComparison.Ordinal)
                || error.EndsWith(" Tags is absent.", StringComparison.Ordinal);
        }

        if (error.StartsWith("Tag '", StringComparison.Ordinal))
            return true;

        return error.EndsWith(" appears before Genesis 12.", StringComparison.Ordinal)
            || error is "Pre-Israelite appears from Genesis 12 onward."
                or "Pre-Israelite appears outside Genesis 1-11.";
    }

    static string CheckRetryMessage(string originalInput, string response, IReadOnlyList<string> errors)
    {
        var lines = new List<string>
        {
            "The check failed: " + string.Join(' ', errors),
            "",
            "Return a full JSON response for the same chapter.",
            "Do not return a patch.",
            "Do not return a Tags catalog.",
            "Each verse tag is one path.",
            "A path starts with People, God, or Places, and a dot separates each step.",
            "Examples: People.Non-Israelite.Moabite, People.Others, Places.Others, Places.Non-Israel.Egypt.Goshen, God.LORD.",
            "Israel the person is People.Israelite.Israel. Israel the land is Places.Israel.",
            "Leave the Subclassifier off. A Subclassifier is minimal, and only when absolutely necessary.",
            "It is necessary only when this chapter has two different people or places that would otherwise have the same path.",
            "A title, a description, or a poetic phrase is not a reason to add one.",
            "A wife, son, family, or other companion is a separate tag. Noah's wife is People.Antediluvian.Noah's wife, not Noah (wife).",
            "A root is a whole path only when it has no fixed children. God is God, not God.God.",
            "People and Places have fixed children. Do not use them alone. Use a leaf, such as People.Israelite or People.Others.",
            "Do not repeat a step. The land is Places.Israel, not Israel.Israel.",
            "Do not put a Subclassifier on People, God, Places, Antediluvian, Pre-Israelite, Israelite, Non-Israelite, Others, Israel, or Non-Israel.",
            "Pre-Israelite is only Genesis 1 through 11. From Genesis 12 onward, use People.Israelite or People.Non-Israelite.",
        };

        lines.Add("");
        lines.Add("Original input:");
        lines.Add(originalInput);
        lines.Add("");
        lines.Add("Your response:");
        lines.Add(response);
        return string.Join('\n', lines);
    }

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

    async Task<StoredTags> LoadNamedTagsAsync(string[] names)
    {
        var stored = new StoredTags();
        if (names.Length == 0)
            return stored;

        var folded = names.Select(name => name.ToLower()).ToArray();
        var rows = await database.VerseTags.Where(tag => folded.Contains(tag.Name.ToLower())).ToListAsync();
        var frontier = new List<int>();
        foreach (var row in rows)
        {
            stored.Rows.Add(row);
            stored.ById[row.Id] = row;
            frontier.Add(row.Id);
        }

        while (frontier.Count > 0)
        {
            var links = await database.VerseTagParents
                .Where(link => frontier.Contains(link.TagId))
                .ToListAsync();
            var missing = new List<int>();
            foreach (var link in links)
            {
                if (!stored.ParentsByChild.TryGetValue(link.TagId, out var list))
                {
                    list = [];
                    stored.ParentsByChild[link.TagId] = list;
                }

                list.Add(link.ParentTagId);
                if (!stored.ById.ContainsKey(link.ParentTagId))
                    missing.Add(link.ParentTagId);
            }

            var needed = missing.Distinct().ToArray();
            if (needed.Length == 0)
                break;

            var parents = await database.VerseTags.Where(tag => needed.Contains(tag.Id)).ToListAsync();
            frontier = [];
            foreach (var parent in parents)
            {
                stored.ById[parent.Id] = parent;
                frontier.Add(parent.Id);
            }
        }

        return stored;
    }

    static VerseTag? FindStoredTag(StoredTags known, TagOutput tag, IReadOnlyList<string> parentNames)
    {
        var modelSub = ChapterCheck.NormalizeSubclassifier(tag.Subclassifier);
        VerseTag? same = null;
        foreach (var row in known.Rows)
        {
            if (!string.Equals(row.Name, tag.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (row.Id == 0)
            {
                if (modelSub.Length == 0 && parentNames.Count == 0 && row.Subclassifier.Length == 0)
                    return row;

                continue;
            }

            if (modelSub.Length > 0)
            {
                if (!string.Equals(row.Subclassifier, modelSub, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            else if (!IsPathKey(row.Subclassifier))
            {
                continue;
            }

            var chain = ParentChain(known, row.Id);
            if (chain is null || !chain.SequenceEqual(parentNames, StringComparer.OrdinalIgnoreCase))
                continue;

            if (string.Equals(row.Name, tag.Name, StringComparison.Ordinal) &&
                string.Equals(row.Subclassifier, modelSub, StringComparison.Ordinal))
                return row;

            same ??= row;
        }

        return same;
    }

    static string StoredKey(TagOutput tag, IReadOnlyList<string> parentNames, StoredTags known)
    {
        var modelSub = ChapterCheck.NormalizeSubclassifier(tag.Subclassifier);
        if (modelSub.Length > 0)
            return modelSub;

        var taken = known.Rows.Any(row => string.Equals(row.Name, tag.Name, StringComparison.OrdinalIgnoreCase));
        return taken ? string.Join('.', parentNames) : "";
    }

    static bool IsPathKey(string subclassifier) =>
        subclassifier.Length == 0
        || subclassifier is "People" or "God" or "Places"
        || subclassifier.Contains('.');

    static List<string>? ParentChain(StoredTags known, int id)
    {
        var names = new List<string>();
        var guard = 0;
        while (known.ParentsByChild.TryGetValue(id, out var parents))
        {
            if (parents.Count != 1 || !known.ById.TryGetValue(parents[0], out var parent))
                return null;

            names.Add(parent.Name);
            id = parents[0];
            if (++guard > 30)
                return null;
        }

        names.Reverse();
        return names;
    }

    sealed class StoredTags
    {
        public List<VerseTag> Rows { get; } = [];
        public Dictionary<int, VerseTag> ById { get; } = [];
        public Dictionary<int, List<int>> ParentsByChild { get; } = [];
    }

    async Task SaveChapterAsync(LabelRun run, SelectedChapter chapter, ChapterOutput output)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        try
        {
            var responseVerses = output.Verses ?? throw new LabelingException("Verses is absent.");
            var byVerse = responseVerses.ToDictionary(verse => verse.VerseNumber);
            var paths = TagPathSet.From(output.Verses);
            if (paths.Errors.Count > 0)
                throw new LabelingException("The check failed: " + string.Join(' ', paths.Errors));

            var names = paths.Tags.Select(tag => tag.Name!).Distinct(StringComparer.Ordinal).ToArray();
            var known = await LoadNamedTagsAsync(names);
            var bySource = new Dictionary<TagOutput, VerseTag>();
            foreach (var tag in paths.Tags)
            {
                var parentNames = paths.ParentNames[tag];
                var row = FindStoredTag(known, tag, parentNames);
                if (row is null)
                {
                    var subclassifier = StoredKey(tag, parentNames, known);
                    row = new VerseTag { Name = tag.Name!, Subclassifier = subclassifier };
                    database.VerseTags.Add(row);
                    known.Rows.Add(row);
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

            var parentLinks = new HashSet<(int Child, int Parent)>();
            foreach (var item in paths.Tags)
            {
                if (!paths.ParentNode.TryGetValue(item, out var parent))
                    continue;

                var link = (bySource[item].Id, bySource[parent].Id);
                if (!parentLinks.Add(link))
                    continue;

                database.VerseTagParents.Add(new VerseTagParent
                {
                    TagId = link.Item1,
                    ParentTagId = link.Item2,
                });
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
                var seenTags = new HashSet<int>();
                var tagIndex = 1;
                foreach (var text in tags)
                {
                    if (!paths.LeafByPath.TryGetValue(text!, out var leaf))
                        throw new LabelingException($"Tag '{text}' is not a path.");

                    var tag = bySource[leaf];
                    if (!seenTags.Add(tag.Id))
                        continue;

                    database.LabelRunTags.Add(new LabelRunTag
                    {
                        LabelRunId = run.Id,
                        VerseId = verse.Id,
                        VerseTagId = tag.Id,
                        TagIndex = tagIndex,
                    });

                    if (!resolvedIds.Contains(verse.Id))
                    {
                        database.VerseTagMappings.Add(new VerseTagMapping
                        {
                            VerseId = verse.Id,
                            VerseTagId = tag.Id,
                            TagIndex = tagIndex,
                        });
                    }

                    tagIndex++;
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

    static string FullMessage(Exception exception)
    {
        var parts = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (parts.Contains(current.Message))
                continue;

            parts.Add(current.Message);
        }

        return string.Join(' ', parts);
    }
}

sealed record SelectedChapter(BibleBook Book, int Chapter, IReadOnlyList<BibleVerse> Verses);
