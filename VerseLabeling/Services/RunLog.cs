using System.Globalization;

namespace VerseLabeling.Services;

sealed class RunLog
{
    static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static readonly JsonSerializerOptions TopicJson = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    readonly string folder;
    bool headerWritten;

    public RunLog(string folder)
    {
        this.folder = folder;
        var csv = Path.Combine(folder, "verses.csv");
        headerWritten = File.Exists(csv) && new FileInfo(csv).Length > 0;
    }

    public void WriteSettings(LabelRun run, int retryDelaySeconds, string? topics = null)
    {
        Directory.CreateDirectory(folder);
        var lines = new List<string>
        {
            $"step={run.Step}",
            $"books={run.Books}",
            $"chapters={run.Chapters}",
        };
        if (topics is not null)
            lines.Add($"topics={topics}");

        lines.Add($"rpm={run.Rpm}");
        lines.Add($"retryDelaySeconds={retryDelaySeconds}");
        lines.Add($"baseUrl={run.BaseUrl}");
        lines.Add($"model={run.Model}");
        lines.Add($"startedUtc={run.RunKey}");
        lines.Add("");
        File.WriteAllText(Path.Combine(folder, "run.txt"), string.Join('\n', lines), Utf8);
    }

    public void WriteTopics(IReadOnlyList<RunTopic> topics)
    {
        Directory.CreateDirectory(folder);
        var json = JsonSerializer.Serialize(
            topics.Select(topic => new TopicFile(topic.Code, topic.Title, topic.Question)),
            TopicJson);
        File.WriteAllText(Path.Combine(folder, "topics.json"), json + "\n", Utf8);
    }

    public void WriteRaw(string bookCode, int chapter, string body, int? attempt = null)
    {
        var directory = Path.Combine(folder, "raw");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"{bookCode}-{chapter}.txt"), body, Utf8);
        if (attempt is int number)
            File.WriteAllText(Path.Combine(directory, $"{bookCode}-{chapter}-{number}.txt"), body, Utf8);
    }

    public void AppendChapter(
        string bookCode,
        int chapter,
        IReadOnlyList<BibleVerse> verses,
        IReadOnlyDictionary<int, VerseOutput> outputByVerse,
        string summary,
        string immediateContext)
    {
        var csv = new StringBuilder();
        if (!headerWritten)
        {
            csv.Append("BookCode,Chapter,VerseNumber,TagIndex,Tag\n");
            headerWritten = true;
        }

        foreach (var verse in verses)
        {
            var tags = outputByVerse[verse.VerseNumber].Tags!;
            for (var index = 0; index < tags.Count; index++)
            {
                csv.Append(Csv(bookCode));
                csv.Append(',');
                csv.Append(chapter);
                csv.Append(',');
                csv.Append(verse.VerseNumber);
                csv.Append(',');
                csv.Append(index + 1);
                csv.Append(',');
                csv.Append(Csv(tags[index]!));
                csv.Append('\n');
            }
        }

        File.AppendAllText(Path.Combine(folder, "verses.csv"), csv.ToString(), Utf8);

        var line = JsonSerializer.Serialize(new ChapterLog(
            bookCode,
            chapter,
            summary,
            immediateContext), JsonOptions);
        File.AppendAllText(Path.Combine(folder, "chapters.jsonl"), line + "\n", Utf8);
    }

    public void AppendTopics(string bookCode, int chapter, IReadOnlyList<TopicVerdict> verdicts)
    {
        var csv = new StringBuilder();
        if (!headerWritten)
        {
            csv.Append("BookCode,Chapter,VerseNumber,Code,Answer,Probability\n");
            headerWritten = true;
        }

        foreach (var verdict in verdicts)
        {
            csv.Append(Csv(bookCode));
            csv.Append(',');
            csv.Append(chapter);
            csv.Append(',');
            csv.Append(verdict.VerseNumber);
            csv.Append(',');
            csv.Append(Csv(verdict.Topic.Code));
            csv.Append(',');
            csv.Append(verdict.Answer);
            csv.Append(',');
            csv.Append(verdict.Probability.ToString("0.############################", CultureInfo.InvariantCulture));
            csv.Append('\n');
        }

        File.AppendAllText(Path.Combine(folder, "verses.csv"), csv.ToString(), Utf8);
    }

    static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

        return value;
    }

    sealed record ChapterLog(string BookCode, int Chapter, string SummarySoFar, string ImmediateContext);

    sealed record TopicFile(string Code, string Title, string Question);
}
