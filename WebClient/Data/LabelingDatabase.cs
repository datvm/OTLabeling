using Microsoft.JSInterop;

namespace WebClient.Data;

public sealed class LabelingDatabase(IJSRuntime js)
{
    public const string DefaultBookOrderCode = "ENG";

    public Task<IReadOnlyList<BookOrderRow>> GetBookOrdersAsync()
        => QueryAsync<BookOrderRow>("SELECT Id, Code, Name FROM BookOrders ORDER BY Id");

    public async Task<IReadOnlyList<BibleBookRow>> GetBooksAsync(string orderCode)
    {
        var books = await QueryAsync<BibleBookRow>(
            $"""
            SELECT b.Id, b.Code, b.Name, t.Name AS Testament
            FROM BibleBooks b
            INNER JOIN BookOrderItems i ON i.BookId = b.Id
            INNER JOIN BookOrders o ON o.Id = i.BookOrderId
            INNER JOIN BookLabelMappings tm ON tm.BookId = b.Id
            INNER JOIN BookLabels t ON t.Id = tm.BookLabelId
            WHERE o.Code = ? AND t.Name IN ('{BibleBookRow.OldTestament}', '{BibleBookRow.NewTestament}')
            ORDER BY i."Order"
            """,
            orderCode);

        var labels = await QueryAsync<BookLabelRow>(
            $"""
            SELECT m.BookId, l.Name
            FROM BookLabelMappings m
            INNER JOIN BookLabels l ON l.Id = m.BookLabelId
            WHERE l.Name NOT IN ('{BibleBookRow.OldTestament}', '{BibleBookRow.NewTestament}')
            ORDER BY l.Id
            """);

        var labelsByBook = labels
            .GroupBy(label => label.BookId)
            .ToDictionary(group => group.Key, group => group.Select(label => label.Name).ToList());

        foreach (var book in books)
        {
            if (labelsByBook.TryGetValue(book.Id, out var names))
                book.Labels = names;
        }

        return books;
    }

    public async Task<BibleBookRow?> GetBookByCodeAsync(string code)
    {
        var rows = await QueryAsync<BibleBookRow>(
            "SELECT Id, Code, Name FROM BibleBooks WHERE Code = ? COLLATE NOCASE LIMIT 1",
            code);
        return rows.FirstOrDefault();
    }

    public async Task<IReadOnlyList<int>> GetChaptersAsync(int bookId)
    {
        var rows = await QueryAsync<BibleChapterRow>(
            "SELECT DISTINCT Chapter FROM BibleVerses WHERE BookId = ? ORDER BY Chapter",
            bookId);
        return rows.Select(row => row.Chapter).ToList();
    }

    public Task<IReadOnlyList<BibleVerseRow>> GetVersesAsync(int bookId, int chapter)
        => QueryAsync<BibleVerseRow>(
            """
            SELECT Chapter, VerseNumber, Text, AccumulatedVerseNumber
            FROM BibleVerses
            WHERE BookId = ? AND Chapter = ?
            ORDER BY VerseNumber
            """,
            bookId,
            chapter);

    sealed class BookLabelRow
    {
        public int BookId { get; set; }
        public string Name { get; set; } = "";
    }

    async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, params object[] parameters)
    {
        var rows = await js.InvokeAsync<List<T>>("appDb.query", sql, parameters);
        return rows;
    }
}
