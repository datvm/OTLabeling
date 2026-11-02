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
            string? bookCode = null;
            string? carriedSummary = null;
            string? carriedImmediate = null;

            foreach (var chapter in chapters)
            {
                current = chapter;
                Console.WriteLine($"{chapter.Book.Code} {chapter.Chapter} started.");
                var (summary, immediate) = ContextFor(chapter, ref bookCode, ref carriedSummary, ref carriedImmediate, settings);
                var request = ChapterPrompt.Build(summary, immediate, chapter.Verses);
                if (requestStart is long started)
                {
                    var remain = gap - Stopwatch.GetElapsedTime(started);
                    if (remain > TimeSpan.Zero)
                        await Task.Delay(remain);
                }

                requestStart = Stopwatch.GetTimestamp();
                var body = await SendAsync(request, settings.RetryDelaySeconds);
                log.WriteRaw(chapter.Book.Code, chapter.Chapter, body);

                var output = ChapterCheck.Parse(body);
                ChapterCheck.DistinctTags(output);
                var errors = ChapterCheck.Check(
                    output,
                    chapter.Book.Code,
                    chapter.Chapter,
                    chapter.BeforeJoshua,
                    chapter.Verses);
                if (errors.Count > 0)
                    throw new LabelingException("The check failed: " + string.Join(' ', errors));

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

    const int MaxContinuousRateLimits = 3;

    int rateLimitStreak;

    async Task<string> SendAsync(string request, int retryDelaySeconds)
    {
        var retryDelay = TimeSpan.FromSeconds(retryDelaySeconds);
        while (true)
        {
            try
            {
                var response = await chat.GetResponseAsync<ChapterOutput>(request, ResponseSchema);
                rateLimitStreak = 0;
                if (string.IsNullOrEmpty(response.Text))
                    throw new LabelingException("The API call returned no body.");

                return response.Text;
            }
            catch (LabelingException)
            {
                throw;
            }
            catch (Exception ex) when (IsTooManyRequests(ex))
            {
                rateLimitStreak++;
                if (rateLimitStreak > MaxContinuousRateLimits)
                    throw new LabelingException("The API call failed: " + Redact(ex.Message));

                Console.WriteLine($"The API returned 429. The next attempt waits {retryDelaySeconds} seconds.");
                await Task.Delay(retryDelay);
            }
            catch (Exception ex)
            {
                throw new LabelingException("The API call failed: " + Redact(ex.Message));
            }
        }
    }

    static bool IsTooManyRequests(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is System.ClientModel.ClientResultException client && client.Status == 429)
                return true;

            if (current is HttpRequestException http && http.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                return true;
        }

        return false;
    }

    async Task<IReadOnlyList<SelectedChapter>> SelectChaptersAsync(RunSettings settings)
    {
        var storedBooks = await database.BibleBooks.ToListAsync();
        var byCode = storedBooks.ToDictionary(book => book.Code, StringComparer.OrdinalIgnoreCase);
        var englishOrder = await EnglishOrderAsync();
        if (!englishOrder.TryGetValue("JOS", out var joshuaOrder))
            throw new LabelingException("The English book order has no Joshua.");

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

            if (!englishOrder.TryGetValue(book.Code, out var bookOrder))
                throw new LabelingException($"{book.Code} is not in the English book order.");

            var beforeJoshua = bookOrder < joshuaOrder;

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
                selected.Add(new SelectedChapter(book, number, byChapter[number], beforeJoshua));
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

    async Task<Dictionary<string, int>> EnglishOrderAsync()
    {
        var rows = await (
            from item in database.BookOrderItems
            join bookOrder in database.BookOrders on item.BookOrderId equals bookOrder.Id
            join book in database.BibleBooks on item.BookId equals book.Id
            where bookOrder.Code == "ENG"
            select new { book.Code, item.Order }
        ).ToListAsync();

        return rows.ToDictionary(row => row.Code, row => row.Order, StringComparer.OrdinalIgnoreCase);
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

    async Task SaveChapterAsync(LabelRun run, SelectedChapter chapter, ChapterOutput output)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        try
        {
            var responseVerses = output.Verses ?? throw new LabelingException("Verses is absent.");
            var byVerse = responseVerses.ToDictionary(verse => verse.VerseNumber);
            var names = (output.Tags ?? [])
                .Select(tag => tag.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var existing = await database.VerseTags
                .Where(tag => names.Contains(tag.Name) && tag.Subclassifier == "")
                .ToListAsync();
            var byName = existing.ToDictionary(tag => tag.Name, StringComparer.Ordinal);
            foreach (var name in names)
            {
                if (byName.ContainsKey(name!))
                    continue;

                var created = new VerseTag { Name = name!, Subclassifier = "" };
                database.VerseTags.Add(created);
                byName.Add(name!, created);
            }

            await database.SaveChangesAsync();

            var touchedIds = names.Select(name => byName[name!].Id).ToArray();
            if (touchedIds.Length > 0)
            {
                await database.VerseTagParents
                    .Where(link => touchedIds.Contains(link.TagId))
                    .ExecuteDeleteAsync();
            }

            foreach (var item in output.Tags ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                    continue;

                var tagId = byName[item.Name].Id;
                foreach (var parentName in item.Parents ?? [])
                {
                    if (string.IsNullOrWhiteSpace(parentName))
                        continue;

                    database.VerseTagParents.Add(new VerseTagParent
                    {
                        TagId = tagId,
                        ParentTagId = byName[parentName].Id,
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
                    var tag = byName[tags[index]!];
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

sealed record SelectedChapter(BibleBook Book, int Chapter, IReadOnlyList<BibleVerse> Verses, bool BeforeJoshua);
