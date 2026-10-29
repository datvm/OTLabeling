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
    public required string BaseUrl { get; init; }
    public required string Model { get; init; }

    public static RunSettings Read(AppConfig config)
    {
        var ai = config.Ai ?? throw new LabelingException("Ai is missing.");
        var labeling = config.VerseLabeling ?? throw new LabelingException("VerseLabeling is missing.");
        var baseUrl = Require(ai.BaseUrl, "BaseUrl");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            throw new LabelingException("BaseUrl is invalid.");

        var model = Require(ai.Model, "Model");
        var step = Require(labeling.Step, "Step");
        if (step != StepName)
            throw new LabelingException($"Step '{step}' has no design file.");

        return new RunSettings
        {
            Step = step,
            BookCodes = ReadBooks(labeling.Books),
            ChaptersText = ReadChapters(labeling.Chapters, out var range),
            Range = range,
            Summary = NormalizeContext(labeling.Summary),
            ImmediateContext = NormalizeContext(labeling.ImmediateContext),
            Rpm = ReadRpm(labeling.Rpm),
            BaseUrl = baseUrl,
            Model = model,
        };
    }

    static string Require(string? value, string name)
    {
        if (value is null)
            throw new LabelingException($"{name} is missing.");

        if (string.IsNullOrWhiteSpace(value))
            throw new LabelingException($"{name} is blank.");

        return value.Trim();
    }

    static IReadOnlyList<string> ReadBooks(string[]? books)
    {
        if (books is null)
            throw new LabelingException("Books is missing.");

        if (books.Length == 0)
            throw new LabelingException("Books is empty.");

        var codes = new List<string>(books.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in books)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new LabelingException("A book code is blank.");

            var code = raw.Trim();
            if (!seen.Add(code))
                throw new LabelingException($"Book code '{code}' is repeated.");

            codes.Add(code);
        }

        return codes;
    }

    static string ReadChapters(string? chapters, out ChapterRange? range)
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

    static int ReadRpm(int? rpm)
    {
        if (rpm is null)
            return 15;

        if (rpm <= 0)
            throw new LabelingException("Rpm must be a positive integer.");

        return rpm.Value;
    }

    static string NormalizeContext(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "None";

        var trimmed = value.Trim();
        return trimmed == "None" ? "None" : trimmed;
    }
}

sealed record ChapterRange(int Start, int? End);
