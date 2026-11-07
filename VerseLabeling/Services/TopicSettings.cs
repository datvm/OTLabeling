namespace VerseLabeling.Services;

sealed class TopicSettings
{
    public required string Step { get; init; }
    public required IReadOnlyList<string> BookCodes { get; init; }
    public required string ChaptersText { get; init; }
    public required ChapterRange? Range { get; init; }
    public required string Summary { get; init; }
    public required string ImmediateContext { get; init; }
    public required int Rpm { get; init; }
    public required int RetryDelaySeconds { get; init; }
    public required string Model { get; init; }
    public required int ResumeRunId { get; init; }
    public required IReadOnlyList<TopicSpec> Topics { get; init; }

    public static TopicSettings Read(AppConfig config)
    {
        var jev = config.Jev ?? throw new LabelingException("Jev is missing.");
        var model = jev.Model;
        if (model is null)
            throw new LabelingException("Jev Model is missing.");

        if (string.IsNullOrWhiteSpace(model))
            throw new LabelingException("Jev Model is blank.");

        var labeling = config.VerseLabeling ?? throw new LabelingException("VerseLabeling is missing.");
        var step = RunSettings.Require(labeling.Step, "Step");
        if (step != TopicLabeler.StepName)
            throw new LabelingException($"Step '{step}' has no design file.");

        var resumeRunId = RunSettings.ReadResume(labeling.ResumeRun);
        IReadOnlyList<string> bookCodes;
        string chaptersText;
        ChapterRange? range;
        IReadOnlyList<TopicSpec> topics;
        if (resumeRunId == 0)
        {
            bookCodes = RunSettings.ReadBooks(labeling.Books);
            chaptersText = RunSettings.ReadChapters(labeling.Chapters, out range);
            topics = ReadTopics(labeling.Topics);
        }
        else
        {
            bookCodes = [];
            chaptersText = "";
            range = null;
            topics = [];
        }

        return new TopicSettings
        {
            Step = step,
            BookCodes = bookCodes,
            ChaptersText = chaptersText,
            Range = range,
            Summary = RunSettings.NormalizeContext(labeling.Summary),
            ImmediateContext = RunSettings.NormalizeContext(labeling.ImmediateContext),
            Rpm = RunSettings.ReadRpm(labeling.Rpm),
            RetryDelaySeconds = RunSettings.ReadRetryDelay(labeling.RetryDelaySeconds),
            Model = model.Trim(),
            ResumeRunId = resumeRunId,
            Topics = topics,
        };
    }

    static IReadOnlyList<TopicSpec> ReadTopics(TopicConfig[]? topics)
    {
        if (topics is null)
            throw new LabelingException("Topics is missing.");

        if (topics.Length == 0)
            throw new LabelingException("Topics is empty.");

        var list = new List<TopicSpec>(topics.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 1;
        foreach (var topic in topics)
        {
            if (topic is null)
                throw new LabelingException("A topic is missing.");

            var code = RunSettings.Require(topic.Code, "Code");
            var title = BlankAsEmpty(topic.Title);
            var question = BlankAsEmpty(topic.Question);
            if (code.Contains(' ') || code.Contains(',') || code.Contains('\n') || code.Contains('\r'))
                throw new LabelingException($"Topic code '{code}' contains a space or a comma.");

            if (!HasWords(code))
                throw new LabelingException("Code has no words.");

            if (!seen.Add(code))
                throw new LabelingException($"Topic code '{code}' is repeated.");

            list.Add(new TopicSpec(code, title, question, order));
            order++;
        }

        return list;
    }

    static string BlankAsEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    static bool HasWords(string text) =>
        text.Any(char.IsLetterOrDigit);
}

sealed record TopicSpec(string Code, string Title, string Question, int SortOrder);

sealed record RunTopic(int Id, string Code, string Title, string Question, int SortOrder);
