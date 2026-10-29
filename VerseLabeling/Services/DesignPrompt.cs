namespace VerseLabeling.Services;

sealed class DesignPrompt
{
    public required string DefinitionText { get; init; }
    public required string OutputRules { get; init; }

    public static DesignPrompt Load(string configDirectory)
    {
        var path = Path.Combine(configDirectory, "VerseLabeling", "Design-Step1.MD");
        if (!File.Exists(path))
            throw new LabelingException("Design-Step1.MD was not found.");

        var lines = File.ReadAllLines(path);
        return new DesignPrompt
        {
            DefinitionText = Copy(lines, "## Definitions", "## Group names", endAtFence: false),
            OutputRules = Copy(lines, "## Output rules", "## Prompt shape", endAtFence: true),
        };
    }

    public string Build(string summary, string immediateContext, IReadOnlyList<BibleVerse> verses)
    {
        var builder = new StringBuilder();
        builder.Append(DefinitionText);
        builder.Append("\n\n");
        builder.Append(OutputRules);
        builder.Append("\n\nSummarySoFar:\n");
        builder.Append(summary);
        builder.Append("\n\nImmediateContext:\n");
        builder.Append(immediateContext);
        builder.Append("\n\nChapter:\n");
        foreach (var verse in verses)
        {
            builder.Append(verse.VerseNumber);
            builder.Append(' ');
            builder.Append(Flatten(verse.Text));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    static string Copy(string[] lines, string heading, string endHeading, bool endAtFence)
    {
        var section = FindLine(lines, 0, lines.Length, heading);
        var limit = FindLine(lines, section + 1, lines.Length, endHeading);
        var marker = FindLine(lines, section + 1, limit, "The first copied line is the next paragraph.");
        var start = NextParagraph(lines, marker + 1, limit);
        var end = endAtFence ? ClosingFence(lines, start, limit) : LastNonEmpty(lines, start, limit);
        return string.Join('\n', lines[start..(end + 1)]);
    }

    static int FindLine(string[] lines, int start, int limit, string text)
    {
        for (var i = start; i < limit; i++)
        {
            if (lines[i].Trim() == text)
                return i;
        }

        throw new LabelingException("Design-Step1.MD is missing the copied text.");
    }

    static int NextParagraph(string[] lines, int start, int limit)
    {
        for (var i = start; i < limit; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                return i;
        }

        throw new LabelingException("Design-Step1.MD is missing the copied text.");
    }

    static int LastNonEmpty(string[] lines, int start, int limit)
    {
        for (var i = limit - 1; i >= start; i--)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
                return i;
        }

        throw new LabelingException("Design-Step1.MD is missing the copied text.");
    }

    static int ClosingFence(string[] lines, int start, int limit)
    {
        var open = -1;
        for (var i = start; i < limit; i++)
        {
            var text = lines[i].Trim();
            if (open < 0)
            {
                if (text.StartsWith("```json", StringComparison.Ordinal))
                    open = i;
            }
            else if (text == "```")
            {
                return i;
            }
        }

        throw new LabelingException("Design-Step1.MD is missing the output rules.");
    }

    static string Flatten(string text) =>
        text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
}
