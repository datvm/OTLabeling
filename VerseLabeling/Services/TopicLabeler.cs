namespace VerseLabeling.Services;

sealed class TopicLabeler(LabelingContext database, ConfigProvider config, string apiKey)
{
    public const string StepName = "label-topics";

    public async Task RunAsync(TopicSettings settings)
    {
        LabelRun? run = null;
        SelectedChapter? current = null;
        try
        {
            List<SelectedChapter> chapters;
            List<RunTopic> topics;
            if (settings.ResumeRunId == 0)
            {
                chapters = (await SelectChaptersAsync(settings)).ToList();
                run = await InsertRunAsync(settings, chapters);
                topics = await LoadTopicsAsync(run.Id);
                Console.WriteLine($"Run {run.Id} started.");
            }
            else
            {
                run = await OpenRunAsync(settings);
                topics = await LoadTopicsAsync(run.Id);
                chapters = await MissingChaptersAsync(run);
                if (chapters.Count == 0)
                {
                    run.Status = LabelRun.StatusCompleted;
                    await database.SaveChangesAsync();
                    Console.WriteLine($"Run {run.Id} has no missing chapters.");
                    return;
                }

                Console.WriteLine($"Run {run.Id} resumed. {chapters.Count} chapters remain.");
            }

            if (topics.Count == 0 || topics.Any(topic => topic.Id <= 0))
                throw new LabelingException($"Run {run.Id} has no topics.");

            Console.WriteLine("Topics: " + string.Join(", ", topics.Select(topic => topic.Code)) + ".");
            var log = new RunLog(Path.Combine(
                config.AppConfigPath,
                "out",
                "verse-labeling",
                run.Step,
                run.RunKey));
            log.WriteSettings(
                run,
                settings.RetryDelaySeconds,
                string.Join(',', topics.Select(topic => topic.Code)));
            log.WriteTopics(topics);

            using var jev = new JevClient(apiKey);
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

            foreach (var chapter in chapters)
            {
                current = chapter;
                Console.WriteLine($"{chapter.Book.Code} {chapter.Chapter} started.");
                var seen = new HashSet<int>();
                foreach (var verse in chapter.Verses)
                {
                    if (!seen.Add(verse.VerseNumber))
                        throw new LabelingException($"Verse {verse.VerseNumber} is repeated in {chapter.Book.Code} {chapter.Chapter}.");
                }

                var (summary, immediate) = await ContextForAsync(chapter);
                var request = TopicPrompt.Build(summary, immediate, chapter.Verses, topics);
                await WaitForTurnAsync();
                var body = await SendAsync(jev, settings.Model, request, settings.RetryDelaySeconds);
                ReportUsage(TopicCheck.ReadUsage(body));
                log.WriteRaw(chapter.Book.Code, chapter.Chapter, Redact(body));
                var response = TopicCheck.Parse(body);
                var result = TopicCheck.Check(response, chapter.Verses, topics);
                if (result.Errors.Count > 0)
                    throw new LabelingException("The check failed: " + string.Join(' ', result.Errors));

                await SaveChapterAsync(run, chapter, topics, result.Verdicts);
                log.AppendTopics(chapter.Book.Code, chapter.Chapter, result.Verdicts);
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

    const int MaxContinuousRetries = int.MaxValue;
    static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMilliseconds(int.MaxValue);

    int retryStreak;
    long totalInputTokens;
    long totalOutputTokens;

    async Task<string> SendAsync(JevClient jev, string model, TopicRequest request, int retryDelaySeconds)
    {
        while (true)
        {
            try
            {
                var body = await jev.AskAsync(model, request);
                retryStreak = 0;
                return body;
            }
            catch (JevRetryException ex)
            {
                if (retryStreak == MaxContinuousRetries)
                {
                    Console.WriteLine($"The API returned {ex.Status}. The retry limit of {MaxContinuousRetries} was reached.");
                    throw new LabelingException("The API call failed: HTTP " + ex.Status.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                retryStreak++;
                var wait = RetryDelay(retryDelaySeconds, retryStreak);
                var waitSeconds = (long)wait.TotalSeconds;
                Console.WriteLine($"The API returned {ex.Status}. Retry {retryStreak}. The next attempt waits {waitSeconds} seconds.");
                await Task.Delay(wait);
            }
        }
    }

    void ReportUsage((long? Input, long? Output) usage)
    {
        if (usage.Input is not long input || usage.Output is not long output)
        {
            Console.WriteLine("Tokens are not available.");
            return;
        }

        totalInputTokens += input;
        totalOutputTokens += output;
        Console.WriteLine(
            $"Tokens in {input} out {output}. Cost is not available. Total in {totalInputTokens} out {totalOutputTokens}. Cost is not available.");
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

    async Task<IReadOnlyList<SelectedChapter>> SelectChaptersAsync(TopicSettings settings)
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
                selected.Add(new SelectedChapter(book, number, byChapter[number], selected.Count + 1, selected.Count == 0));
        }

        return selected;
    }

    async Task<LabelRun> OpenRunAsync(TopicSettings settings)
    {
        var run = await database.LabelRuns.FirstOrDefaultAsync(item => item.Id == settings.ResumeRunId);
        if (run is null)
            throw new LabelingException($"Run {settings.ResumeRunId} does not exist.");

        if (!run.Step.Equals(settings.Step, StringComparison.Ordinal))
            throw new LabelingException($"Run {run.Id} is step '{run.Step}'.");

        database.LabelRuns.Attach(run);
        run.Status = LabelRun.StatusRunning;
        run.Rpm = settings.Rpm;
        run.BaseUrl = JevClient.BaseUrl;
        run.Model = settings.Model;
        await database.SaveChangesAsync();
        return run;
    }

    async Task<List<RunTopic>> LoadTopicsAsync(int runId)
    {
        var rows = await database.LabelRunTopics
            .Where(topic => topic.LabelRunId == runId)
            .OrderBy(topic => topic.SortOrder)
            .ToListAsync();
        if (rows.Count == 0)
            throw new LabelingException($"Run {runId} has no topics.");

        return rows
            .Select(topic => new RunTopic(topic.Id, topic.Code, topic.Title, topic.Question, topic.SortOrder))
            .ToList();
    }

    async Task<List<SelectedChapter>> MissingChaptersAsync(LabelRun run)
    {
        var plans = await database.LabelRunChapters
            .Where(row => row.LabelRunId == run.Id)
            .OrderBy(row => row.SortOrder)
            .ToListAsync();
        if (plans.Count == 0)
            throw new LabelingException($"Run {run.Id} has no chapter plan.");

        var pending = plans.Where(row => !row.Filled).ToList();
        if (pending.Count == 0)
            return [];

        var books = await database.BibleBooks.ToDictionaryAsync(book => book.Id);
        var firstOrder = plans[0].SortOrder;
        var selected = new List<SelectedChapter>();
        foreach (var plan in pending)
        {
            if (!books.TryGetValue(plan.BookId, out var book))
                throw new LabelingException($"Run {run.Id} names a missing book.");

            var verses = await database.BibleVerses
                .Where(verse => verse.BookId == plan.BookId && verse.Chapter == plan.Chapter)
                .OrderBy(verse => verse.VerseNumber)
                .ToListAsync();
            if (verses.Count == 0)
                throw new LabelingException($"Chapter {plan.Chapter} is not in {book.Code}.");

            selected.Add(new SelectedChapter(
                book,
                plan.Chapter,
                verses,
                plan.SortOrder,
                plan.SortOrder == firstOrder));
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

    async Task<LabelRun> InsertRunAsync(TopicSettings settings, IReadOnlyList<SelectedChapter> chapters)
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
            BaseUrl = JevClient.BaseUrl,
            Model = settings.Model,
            Status = LabelRun.StatusRunning,
        };

        await using var transaction = await database.Database.BeginTransactionAsync();
        try
        {
            var resolved = await ResolveTopicsAsync(settings.Topics);
            database.LabelRuns.Add(run);
            await database.SaveChangesAsync();
            var order = 1;
            foreach (var chapter in chapters)
            {
                database.LabelRunChapters.Add(new LabelRunChapter
                {
                    LabelRunId = run.Id,
                    BookId = chapter.Book.Id,
                    Chapter = chapter.Chapter,
                    SortOrder = order,
                });
                order++;
            }

            foreach (var topic in resolved)
            {
                database.LabelRunTopics.Add(new LabelRunTopic
                {
                    LabelRunId = run.Id,
                    SortOrder = topic.SortOrder,
                    Code = topic.Code,
                    Title = topic.Title,
                    Question = topic.Question,
                });
            }

            await database.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            if (database.Entry(run).State != EntityState.Detached)
                database.Entry(run).State = EntityState.Detached;

            run.Id = 0;
            DetachExceptRun();
            if (ex is LabelingException)
                throw;

            var detail = ex is DbUpdateException update
                ? update.InnerException?.Message ?? update.Message
                : ex.Message;
            if (detail.Contains("Topics.Code", StringComparison.OrdinalIgnoreCase))
                throw new LabelingException("A topic code already exists.");

            if (detail.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                throw new LabelingException("A run with this RunKey already exists.");

            throw new LabelingException("The database write failed: " + detail);
        }

        DetachExceptRun();
        return run;
    }

    async Task<IReadOnlyList<TopicSpec>> ResolveTopicsAsync(IReadOnlyList<TopicSpec> topics)
    {
        var codes = topics.Select(topic => topic.Code).ToArray();
        var rows = await database.Topics
            .Where(row => codes.Contains(row.Code))
            .ToListAsync();
        var byCode = new Dictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
            byCode[row.Code] = row;

        var resolved = new List<TopicSpec>(topics.Count);
        foreach (var topic in topics)
        {
            if (!byCode.TryGetValue(topic.Code, out var row))
            {
                var title = RequiredText(topic.Title, topic.Code, "title");
                var question = RequiredText(topic.Question, topic.Code, "question");
                row = new Topic
                {
                    Code = topic.Code,
                    Title = title,
                    Question = question,
                };
                database.Topics.Add(row);
                byCode[row.Code] = row;
            }
            else
            {
                database.Topics.Attach(row);
                if (HasWords(topic.Title))
                    row.Title = topic.Title;
                if (HasWords(topic.Question))
                    row.Question = topic.Question;

                row.Title = RequiredText(row.Title, topic.Code, "title");
                row.Question = RequiredText(row.Question, topic.Code, "question");
            }

            resolved.Add(new TopicSpec(row.Code, row.Title, row.Question, topic.SortOrder));
        }

        return resolved;
    }

    static string RequiredText(string text, string code, string field)
    {
        if (!HasWords(text))
            throw new LabelingException($"Topic '{code}' has a blank {field}.");

        return text.Trim();
    }

    async Task SaveChapterAsync(
        LabelRun run,
        SelectedChapter chapter,
        IReadOnlyList<RunTopic> topics,
        IReadOnlyList<TopicVerdict> verdicts)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        try
        {
            var byVerse = verdicts.ToLookup(verdict => verdict.VerseNumber);
            foreach (var verse in chapter.Verses)
            {
                var rows = byVerse[verse.VerseNumber].ToList();
                if (rows.Count != topics.Count)
                    throw new LabelingException($"Verse {verse.VerseNumber} topic count is {rows.Count}.");

                foreach (var verdict in rows)
                {
                    database.LabelRunTopicAnswers.Add(new LabelRunTopicAnswer
                    {
                        LabelRunId = run.Id,
                        VerseId = verse.Id,
                        LabelRunTopicId = verdict.Topic.Id,
                        Answer = verdict.Answer,
                        Probability = verdict.Probability,
                    });
                }
            }

            var verseIds = chapter.Verses.Select(verse => verse.Id).ToArray();
            var codes = topics.Select(topic => topic.Code).ToArray();
            await database.VerseTopicAnswers
                .Where(row => verseIds.Contains(row.VerseId) && codes.Contains(row.Code))
                .ExecuteDeleteAsync();

            foreach (var verse in chapter.Verses)
            {
                foreach (var verdict in byVerse[verse.VerseNumber])
                {
                    database.VerseTopicAnswers.Add(new VerseTopicAnswer
                    {
                        VerseId = verse.Id,
                        Code = verdict.Topic.Code,
                        Answer = verdict.Answer,
                        Probability = verdict.Probability,
                        LabelRunId = run.Id,
                    });
                }
            }

            var plan = await database.LabelRunChapters
                .Where(row => row.LabelRunId == run.Id && row.BookId == chapter.Book.Id && row.Chapter == chapter.Chapter)
                .FirstAsync();
            database.LabelRunChapters.Attach(plan);
            plan.Filled = true;
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

    async Task<(string? Summary, string? Immediate)> ContextForAsync(SelectedChapter chapter)
    {
        if (chapter.Chapter <= 1)
            return (null, null);

        var stored = await database.ChapterContexts
            .Where(row => row.BookId == chapter.Book.Id && row.Chapter == chapter.Chapter - 1)
            .FirstOrDefaultAsync();
        if (stored is null || !HasWords(stored.SummarySoFar) || !HasWords(stored.ImmediateContext))
            return (null, null);

        return (stored.SummarySoFar, stored.ImmediateContext);
    }

    static bool HasWords(string? text) =>
        text?.Any(char.IsLetterOrDigit) == true;

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
