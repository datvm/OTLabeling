namespace VerseLabeling.Services;

sealed class RunLog
{
    static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    readonly string folder;
    bool headerWritten;

    public RunLog(string folder)
    {
        this.folder = folder;
    }

    public void WriteSettings(LabelRun run)
    {
        Directory.CreateDirectory(folder);
        var text = string.Join('\n',
        [
            $"step={run.Step}",
            $"books={run.Books}",
            $"chapters={run.Chapters}",
            $"rpm={run.Rpm}",
            $"baseUrl={run.BaseUrl}",
            $"model={run.Model}",
            $"startedUtc={run.RunKey}",
            "",
        ]);
        File.WriteAllText(Path.Combine(folder, "run.txt"), text, Utf8);
    }

    public void WriteRaw(string bookCode, int chapter, string body)
    {
        var directory = Path.Combine(folder, "raw");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"{bookCode}-{chapter}.txt"), body, Utf8);
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

    static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

        return value;
    }

    sealed record ChapterLog(string BookCode, int Chapter, string SummarySoFar, string ImmediateContext);
}
