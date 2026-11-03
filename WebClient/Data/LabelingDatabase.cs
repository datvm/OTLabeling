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

        await AttachLabelsAsync(books);
        return books;
    }

    public async Task<BibleBookRow?> GetBookByCodeAsync(string code)
    {
        var rows = await QueryAsync<BibleBookRow>(
            "SELECT Id, Code, Name FROM BibleBooks WHERE Code = ? COLLATE NOCASE LIMIT 1",
            code);
        var book = rows.FirstOrDefault();
        if (book is not null)
            await AttachLabelsAsync([book]);

        return book;
    }

    public async Task<IReadOnlyList<int>> GetChaptersAsync(int bookId)
    {
        var rows = await QueryAsync<BibleChapterRow>(
            "SELECT DISTINCT Chapter FROM BibleVerses WHERE BookId = ? ORDER BY Chapter",
            bookId);
        return rows.Select(row => row.Chapter).ToList();
    }

    public async Task<IReadOnlyList<BibleVerseRow>> GetVersesAsync(int bookId)
    {
        var verses = await QueryAsync<BibleVerseRow>(
            """
            SELECT Chapter, VerseNumber, Text, AccumulatedVerseNumber
            FROM BibleVerses
            WHERE BookId = ?
            ORDER BY Chapter, VerseNumber
            """,
            bookId);
        await AttachVerseTagsAsync(bookId, null, verses);
        return verses;
    }

    public async Task<IReadOnlyList<BibleVerseRow>> GetVersesAsync(int bookId, int chapter)
    {
        var verses = await QueryAsync<BibleVerseRow>(
            """
            SELECT Chapter, VerseNumber, Text, AccumulatedVerseNumber
            FROM BibleVerses
            WHERE BookId = ? AND Chapter = ?
            ORDER BY VerseNumber
            """,
            bookId,
            chapter);
        await AttachVerseTagsAsync(bookId, chapter, verses);
        return verses;
    }

    public Task<IReadOnlyList<VerseTagRow>> GetVerseTagsAsync()
        => QueryAsync<VerseTagRow>(
            """
            WITH RECURSIVE Walk AS (
                SELECT Id AS RootId, Id AS TagId FROM VerseTags
                UNION
                SELECT walk.RootId, child.TagId
                FROM VerseTagParents child
                INNER JOIN Walk walk ON walk.TagId = child.ParentTagId
            )
            SELECT t.Id, t.Name, t.Subclassifier, COUNT(DISTINCT m.VerseId) AS VerseCount
            FROM VerseTags t
            LEFT JOIN Walk walk ON walk.RootId = t.Id
            LEFT JOIN VerseTagMappings m ON m.VerseTagId = walk.TagId
            GROUP BY t.Id, t.Name, t.Subclassifier
            ORDER BY t.Name, t.Subclassifier
            """);

    public async Task<VerseTagRow?> GetVerseTagAsync(int id)
    {
        var rows = await QueryAsync<VerseTagRow>(
            """
            WITH RECURSIVE Walk AS (
                SELECT Id AS RootId, Id AS TagId FROM VerseTags
                UNION
                SELECT walk.RootId, child.TagId
                FROM VerseTagParents child
                INNER JOIN Walk walk ON walk.TagId = child.ParentTagId
            )
            SELECT t.Id, t.Name, t.Subclassifier, COUNT(DISTINCT m.VerseId) AS VerseCount
            FROM VerseTags t
            LEFT JOIN Walk walk ON walk.RootId = t.Id
            LEFT JOIN VerseTagMappings m ON m.VerseTagId = walk.TagId
            WHERE t.Id = ?
            GROUP BY t.Id, t.Name, t.Subclassifier
            """,
            id);
        return rows.FirstOrDefault();
    }

    public Task<IReadOnlyList<VerseTagParentLink>> GetVerseTagParentLinksAsync()
        => QueryAsync<VerseTagParentLink>(
            """
            SELECT link.TagId, link.ParentTagId
            FROM VerseTagParents link
            """);

    public Task<IReadOnlyList<TagVerseLink>> GetTagVerseLinksAsync()
        => QueryAsync<TagVerseLink>(
            """
            SELECT m.VerseId, v.BookId, m.VerseTagId AS TagId
            FROM VerseTagMappings m
            INNER JOIN BibleVerses v ON v.Id = m.VerseId
            """);

    public Task<IReadOnlyList<VerseTagRef>> GetVerseTagParentsAsync(int tagId)
        => QueryAsync<VerseTagRef>(
            """
            SELECT p.Id, p.Name, p.Subclassifier
            FROM VerseTagParents link
            INNER JOIN VerseTags p ON p.Id = link.ParentTagId
            WHERE link.TagId = ?
            ORDER BY p.Name
            """,
            tagId);

    public async Task<IReadOnlyList<TaggedVerseRow>> GetVersesForTagAsync(int tagId)
    {
        var rows = await QueryAsync<TaggedVerseLinkRow>(
            """
            WITH RECURSIVE Descendants AS (
                SELECT Id AS TagId FROM VerseTags WHERE Id = ?
                UNION
                SELECT child.TagId
                FROM VerseTagParents child
                INNER JOIN Descendants parent ON parent.TagId = child.ParentTagId
            )
            SELECT b.Id AS BookId, b.Code AS BookCode, b.Name AS BookName,
                   v.Id AS VerseId, v.Chapter, v.VerseNumber, v.Text,
                   t.Id AS TagId, t.Name AS TagName, t.Subclassifier AS TagSubclassifier
            FROM BibleVerses v
            INNER JOIN BibleBooks b ON b.Id = v.BookId
            INNER JOIN BookOrderItems i ON i.BookId = b.Id
            INNER JOIN BookOrders o ON o.Id = i.BookOrderId
            INNER JOIN VerseTagMappings m ON m.VerseId = v.Id
            INNER JOIN VerseTags t ON t.Id = m.VerseTagId
            WHERE o.Code = ?
              AND v.Id IN (
                  SELECT mapping.VerseId
                  FROM VerseTagMappings mapping
                  WHERE mapping.VerseTagId IN (SELECT TagId FROM Descendants)
              )
            ORDER BY i."Order", v.Chapter, v.VerseNumber, m.TagIndex
            """,
            tagId,
            DefaultBookOrderCode);

        var parents = await ParentIdsByChildAsync();
        var verses = new List<TaggedVerseRow>();
        foreach (var group in rows.GroupBy(row => row.VerseId))
        {
            var first = group.First();
            verses.Add(new TaggedVerseRow
            {
                BookId = first.BookId,
                BookCode = first.BookCode,
                BookName = first.BookName,
                Chapter = first.Chapter,
                VerseNumber = first.VerseNumber,
                Text = first.Text,
                Tags = OrganizeTags(
                    group.Select(row => new VerseTagRef
                    {
                        Id = row.TagId,
                        Name = row.TagName,
                        Subclassifier = row.TagSubclassifier,
                    }).ToList(),
                    parents),
            });
        }

        return verses;
    }

    async Task AttachLabelsAsync(IReadOnlyList<BibleBookRow> books)
    {
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
    }

    sealed class BookLabelRow
    {
        public int BookId { get; set; }
        public string Name { get; set; } = "";
    }

    sealed class VerseTagLinkRow
    {
        public int Chapter { get; set; }
        public int VerseNumber { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Subclassifier { get; set; } = "";
    }

    sealed class TaggedVerseLinkRow
    {
        public int BookId { get; set; }
        public string BookCode { get; set; } = "";
        public string BookName { get; set; } = "";
        public int VerseId { get; set; }
        public int Chapter { get; set; }
        public int VerseNumber { get; set; }
        public string Text { get; set; } = "";
        public int TagId { get; set; }
        public string TagName { get; set; } = "";
        public string TagSubclassifier { get; set; } = "";
    }

    async Task AttachVerseTagsAsync(int bookId, int? chapter, IReadOnlyList<BibleVerseRow> verses)
    {
        var links = chapter is int number
            ? await QueryAsync<VerseTagLinkRow>(
                """
                SELECT v.Chapter, v.VerseNumber, t.Id, t.Name, t.Subclassifier
                FROM VerseTagMappings m
                INNER JOIN VerseTags t ON t.Id = m.VerseTagId
                INNER JOIN BibleVerses v ON v.Id = m.VerseId
                WHERE v.BookId = ? AND v.Chapter = ?
                ORDER BY v.VerseNumber, m.TagIndex
                """,
                bookId,
                number)
            : await QueryAsync<VerseTagLinkRow>(
                """
                SELECT v.Chapter, v.VerseNumber, t.Id, t.Name, t.Subclassifier
                FROM VerseTagMappings m
                INNER JOIN VerseTags t ON t.Id = m.VerseTagId
                INNER JOIN BibleVerses v ON v.Id = m.VerseId
                WHERE v.BookId = ?
                ORDER BY v.Chapter, v.VerseNumber, m.TagIndex
                """,
                bookId);

        var parents = await ParentIdsByChildAsync();
        var tagsByVerse = links
            .GroupBy(link => (link.Chapter, link.VerseNumber))
            .ToDictionary(
                group => group.Key,
                group => OrganizeTags(
                    group.Select(link => new VerseTagRef
                    {
                        Id = link.Id,
                        Name = link.Name,
                        Subclassifier = link.Subclassifier,
                    }).ToList(),
                    parents));

        foreach (var verse in verses)
        {
            if (tagsByVerse.TryGetValue((verse.Chapter, verse.VerseNumber), out var tags))
                verse.Tags = tags;
        }
    }

    sealed class ParentLinkRow
    {
        public int TagId { get; set; }
        public int ParentTagId { get; set; }
    }

    async Task<Dictionary<int, List<int>>> ParentIdsByChildAsync()
    {
        var links = await QueryAsync<ParentLinkRow>(
            """
            SELECT link.TagId, link.ParentTagId
            FROM VerseTagParents link
            INNER JOIN VerseTags parent ON parent.Id = link.ParentTagId
            ORDER BY parent.Name
            """);

        var parents = new Dictionary<int, List<int>>();
        foreach (var link in links)
        {
            if (!parents.TryGetValue(link.TagId, out var parentIds))
            {
                parentIds = [];
                parents.Add(link.TagId, parentIds);
            }

            parentIds.Add(link.ParentTagId);
        }

        return parents;
    }

    static List<List<VerseTagRef>> OrganizeTags(
        List<VerseTagRef> tags,
        IReadOnlyDictionary<int, List<int>> parentsByChild)
    {
        var ordered = new List<VerseTagRef>();
        var seen = new HashSet<int>();
        foreach (var tag in tags)
        {
            if (seen.Add(tag.Id))
                ordered.Add(tag);
        }

        var onVerse = ordered.ToDictionary(tag => tag.Id);
        var hasChild = new HashSet<int>();
        foreach (var tag in ordered)
        {
            if (!parentsByChild.TryGetValue(tag.Id, out var parentIds))
                continue;

            foreach (var parentId in parentIds)
            {
                if (onVerse.ContainsKey(parentId))
                    hasChild.Add(parentId);
            }
        }

        var chains = new List<List<VerseTagRef>>();
        foreach (var tag in ordered)
        {
            if (hasChild.Contains(tag.Id))
                continue;

            CollectChains(tag, [], [], onVerse, parentsByChild, chains);
        }

        return chains;
    }

    static void CollectChains(
        VerseTagRef tag,
        List<VerseTagRef> stack,
        HashSet<int> trail,
        Dictionary<int, VerseTagRef> onVerse,
        IReadOnlyDictionary<int, List<int>> parentsByChild,
        List<List<VerseTagRef>> chains)
    {
        if (!trail.Add(tag.Id))
            return;

        stack.Add(tag);
        var parents = new List<VerseTagRef>();
        if (parentsByChild.TryGetValue(tag.Id, out var parentIds))
        {
            foreach (var parentId in parentIds)
            {
                if (onVerse.TryGetValue(parentId, out var parent))
                    parents.Add(parent);
            }
        }

        if (parents.Count == 0)
        {
            var chain = stack.ToList();
            chain.Reverse();
            chains.Add(chain);
        }
        else
        {
            foreach (var parent in parents)
                CollectChains(parent, stack, trail, onVerse, parentsByChild, chains);
        }

        stack.RemoveAt(stack.Count - 1);
        trail.Remove(tag.Id);
    }

    async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, params object[] parameters)
    {
        var rows = await js.InvokeAsync<List<T>>("appDb.query", sql, parameters);
        return rows;
    }
}
