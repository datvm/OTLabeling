using System.Text.Json;
using Common;
using Common.Entities;
using Microsoft.EntityFrameworkCore;

var config = new ConfigProvider();
await config.LoadAsync();

var options = new DbContextOptionsBuilder<LabelingContext>();
LabelingContext.Configure(options, config);
await using var database = new LabelingContext(options.Options);
await database.Database.MigrateAsync();

var root = Path.Combine(config.AppConfigPath, "out", "verse-labeling");
var files = Directory.Exists(root)
    ? Directory.GetFiles(root, "chapters.jsonl", SearchOption.AllDirectories)
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToList()
    : [];
if (files.Count == 0)
    throw new InvalidOperationException($"No chapters.jsonl files are under '{root}'.");

var latest = new Dictionary<string, LogChapter>(StringComparer.Ordinal);
var skipped = 0;
foreach (var file in files)
{
    foreach (var line in File.ReadLines(file))
    {
        var text = line.Trim('\0', ' ', '\t', '\r');
        if (text.Length == 0 || text[0] != '{')
        {
            skipped++;
            continue;
        }

        LogChapter row;
        try
        {
            row = JsonSerializer.Deserialize<LogChapter>(text)
                ?? throw new InvalidOperationException($"A log line in '{file}' is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"A log line in '{file}' is not JSON.", ex);
        }
        if (!HasWords(row.SummarySoFar) || !HasWords(row.ImmediateContext))
        {
            skipped++;
            continue;
        }

        latest[$"{row.BookCode}\u001f{row.Chapter}"] = row;
    }
}

var books = await database.BibleBooks.ToDictionaryAsync(
    book => book.Code,
    book => book.Id,
    StringComparer.OrdinalIgnoreCase);
var rows = new List<ChapterContext>();
foreach (var row in latest.Values)
{
    if (!books.TryGetValue(row.BookCode, out var bookId))
        throw new InvalidOperationException($"The log names book '{row.BookCode}', which is not in the database.");

    rows.Add(new ChapterContext
    {
        BookId = bookId,
        Chapter = row.Chapter,
        SummarySoFar = row.SummarySoFar,
        ImmediateContext = row.ImmediateContext,
    });
}

await using var transaction = await database.Database.BeginTransactionAsync();
await database.ChapterContexts.ExecuteDeleteAsync();
database.ChapterContexts.AddRange(rows);
await database.SaveChangesAsync();
await transaction.CommitAsync();

Console.WriteLine($"Copied {rows.Count} chapter contexts from {files.Count} logs. Skipped {skipped} lines with no words.");

static bool HasWords(string? text) =>
    text?.Any(char.IsLetterOrDigit) == true;

sealed class LogChapter
{
    public string BookCode { get; set; } = "";
    public int Chapter { get; set; }
    public string SummarySoFar { get; set; } = "";
    public string ImmediateContext { get; set; } = "";
}
