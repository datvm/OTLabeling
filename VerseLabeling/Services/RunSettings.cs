namespace VerseLabeling.Services;

sealed class RunSettings
{
    public const string StepName = "label-is-israelites";

    public required string Step { get; init; }
    public required IReadOnlyList<string> BookCodes { get; init; }
    public required string ChaptersText { get; init; }
    public required ChapterRange? Range { get; init; }
    public required string Summary { get; init; }
    public required string ImmediateContext { get; init; }
    public required int Rpm { get; init; }
    public required int RetryDelaySeconds { get; init; }
    public required string BaseUrl { get; init; }
    public required string Model { get; init; }
    public required int ResumeRunId { get; init; }
    public required decimal? InputTokenCost { get; init; }
    public required decimal? OutputTokenCost { get; init; }
    public required string? ReasoningEffort { get; init; }

    public static RunSettings Read(AppConfig config)
    {
        var ai = config.Ai ?? throw new LabelingException("Ai is missing.");
        var labeling = config.VerseLabeling ?? throw new LabelingException("VerseLabeling is missing.");
        var baseUrl = Require(ai.BaseUrl, "BaseUrl");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            throw new LabelingException("BaseUrl is invalid.");

        var model = Require(ai.Model, "Model");
        var (inputTokenCost, outputTokenCost) = ReadTokenCost(ai.InputTokenCost, ai.OutputTokenCost);
        var reasoningEffort = ReadReasoning(ai.ReasoningEffort);
        var step = Require(labeling.Step, "Step");
        if (step != StepName)
            throw new LabelingException($"Step '{step}' has no design file.");

        var resumeRunId = ReadResume(labeling.ResumeRun);
        IReadOnlyList<string> bookCodes;
        string chaptersText;
        ChapterRange? range;
        if (resumeRunId == 0)
        {
            bookCodes = ReadBooks(labeling.Books);
            chaptersText = ReadChapters(labeling.Chapters, out range);
        }
        else
        {
            bookCodes = [];
            chaptersText = "";
            range = null;
        }

        return new RunSettings
        {
            Step = step,
            BookCodes = bookCodes,
            ChaptersText = chaptersText,
            Range = range,
            Summary = NormalizeContext(labeling.Summary),
            ImmediateContext = NormalizeContext(labeling.ImmediateContext),
            Rpm = ReadRpm(labeling.Rpm),
            RetryDelaySeconds = ReadRetryDelay(labeling.RetryDelaySeconds),
            BaseUrl = baseUrl,
            Model = model,
            ResumeRunId = resumeRunId,
            InputTokenCost = inputTokenCost,
            OutputTokenCost = outputTokenCost,
            ReasoningEffort = reasoningEffort,
        };
    }

    public static string? ReadReasoning(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var effort = value.Trim();
        if (effort is "none" or "minimal" or "low" or "medium" or "high" or "xhigh" or "max")
            return effort;

        throw new LabelingException("ReasoningEffort must be none, minimal, low, medium, high, xhigh, or max.");
    }

    static (decimal? Input, decimal? Output) ReadTokenCost(float? input, float? output)
    {
        if (input is null && output is null)
            return (null, null);

        if (input is null || output is null)
            throw new LabelingException("Set both InputTokenCost and OutputTokenCost, or neither.");

        return (ReadRate(input.Value, "InputTokenCost"), ReadRate(output.Value, "OutputTokenCost"));
    }

    static decimal ReadRate(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
            throw new LabelingException($"{name} must be zero or greater.");

        return (decimal)value;
    }

    internal static int ReadResume(int? resumeRun)
    {
        if (resumeRun is null or 0)
            return 0;

        if (resumeRun < 0)
            throw new LabelingException("ResumeRun must be zero or a run id.");

        return resumeRun.Value;
    }

    internal static string Require(string? value, string name)
    {
        if (value is null)
            throw new LabelingException($"{name} is missing.");

        if (string.IsNullOrWhiteSpace(value))
            throw new LabelingException($"{name} is blank.");

        return value.Trim();
    }

    static readonly string[] EnglishBookOrder =
    [
        "GEN", "EXO", "LEV", "NUM", "DEU",
        "JOS", "JDG", "RUT", "1SA", "2SA", "1KI", "2KI", "1CH", "2CH", "EZR", "NEH", "EST",
        "JOB", "PSA", "PRO", "ECC", "SNG",
        "ISA", "JER", "LAM", "EZK", "DAN",
        "HOS", "JOL", "AMO", "OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL",
        "MAT", "MRK", "LUK", "JHN", "ACT",
        "ROM", "1CO", "2CO", "GAL", "EPH", "PHP", "COL", "1TH", "2TH", "1TI", "2TI", "TIT", "PHM",
        "HEB", "JAS", "1PE", "2PE", "1JN", "2JN", "3JN", "JUD",
        "REV",
    ];

    internal static IReadOnlyList<string> ReadBooks(string[]? books)
    {
        if (books is null)
            throw new LabelingException("Books is missing.");

        if (books.Length == 0)
            throw new LabelingException("Books is empty.");

        if (books.Length == 1 && string.Equals(books[0]?.Trim(), "*", StringComparison.Ordinal))
            return EnglishBookOrder;

        var codes = new List<string>(books.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in books)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new LabelingException("A book code is blank.");

            var code = raw.Trim();
            if (code == "*")
                throw new LabelingException("Books '*' must be the only value.");

            if (!seen.Add(code))
                throw new LabelingException($"Book code '{code}' is repeated.");

            codes.Add(code);
        }

        return codes;
    }

    internal static string ReadChapters(string? chapters, out ChapterRange? range)
    {
        if (string.IsNullOrWhiteSpace(chapters))
        {
            range = null;
            return "all";
        }

        var text = chapters.Trim();
        if (text.Contains(' '))
            throw new LabelingException("Chapters contains a space.");

        if (text.Contains(','))
            throw new LabelingException("A comma list of ranges is not allowed.");

        var dash = text.IndexOf('-');
        if (dash < 0)
            throw new LabelingException("A single chapter number is not a range.");

        if (dash == 0 || text.IndexOf('-', dash + 1) >= 0)
            throw new LabelingException("Chapters is invalid.");

        if (!TryChapter(text[..dash], out var start))
            throw new LabelingException("Chapters is invalid.");

        var right = text[(dash + 1)..];
        int? end = null;
        if (right.Length > 0)
        {
            if (!TryChapter(right, out var endValue))
                throw new LabelingException("Chapters is invalid.");

            if (endValue < start)
                throw new LabelingException("A closed range ends before it starts.");

            end = endValue;
        }

        range = new ChapterRange(start, end);
        return text;
    }

    static bool TryChapter(string text, out int number)
    {
        number = 0;
        if (text.Length == 0 || !text.All(char.IsAsciiDigit))
            return false;

        return int.TryParse(text, out number) && number >= 1;
    }

    internal static int ReadRpm(int? rpm)
    {
        if (rpm is null)
            return 15;

        if (rpm <= 0)
            throw new LabelingException("Rpm must be a positive integer.");

        return rpm.Value;
    }

    internal static int ReadRetryDelay(int? seconds)
    {
        if (seconds is null)
            return 30;

        if (seconds <= 0)
            throw new LabelingException("RetryDelaySeconds must be a positive integer.");

        return seconds.Value;
    }

    internal static string NormalizeContext(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "None";

        var trimmed = value.Trim();
        return trimmed == "None" ? "None" : trimmed;
    }
}

sealed record ChapterRange(int Start, int? End);
