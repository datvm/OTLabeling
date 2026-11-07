using System.Globalization;

namespace WebClient.Data;

public sealed class TagFilterSelection
{
    const int SuggestionLimit = 8;

    public string Keyword { get; set; } = "";
    public string TagQuery { get; set; } = "";
    public bool BooksOpen { get; set; }
    public HashSet<int> BookIds { get; } = [];
    public List<int> TagIds { get; } = [];
    public bool MatchAnyTag { get; set; }
    public List<VerseTagRow> SelectedTags { get; private set; } = [];
    public List<VerseTagRow> Suggestions { get; set; } = [];

    public bool HasVerseFilter => BookIds.Count > 0 || TagIds.Count > 0;

    public bool HasAnyFilter => Keyword.Trim().Length > 0 || HasVerseFilter;

    public bool ApplyQuery(
        string? keyword,
        IReadOnlyList<string>? bookCodes,
        IReadOnlyList<int>? tagIds,
        string? tagOperator,
        IReadOnlyList<BibleBookRow> books,
        IReadOnlyDictionary<int, VerseTagRow> tagsById)
    {
        var nextKeyword = keyword?.Trim() ?? "";
        var nextBooks = new HashSet<int>();
        if (bookCodes is not null)
        {
            foreach (var code in bookCodes)
            {
                var book = books.FirstOrDefault(item =>
                    string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
                if (book is not null)
                    nextBooks.Add(book.Id);
            }
        }

        var nextTags = new List<int>();
        if (tagIds is not null)
        {
            foreach (var id in tagIds)
            {
                if (tagsById.ContainsKey(id) && !nextTags.Contains(id))
                    nextTags.Add(id);
            }
        }

        var nextAny = string.Equals(tagOperator, "or", StringComparison.OrdinalIgnoreCase);
        var sameKeyword = string.Equals(Keyword.Trim(), nextKeyword, StringComparison.Ordinal);
        var sameBooks = BookIds.SetEquals(nextBooks);
        var sameTags = TagIds.SequenceEqual(nextTags);
        var sameOperator = MatchAnyTag == nextAny;
        if (sameKeyword && sameBooks && sameTags && sameOperator)
            return false;

        if (!sameKeyword)
            Keyword = nextKeyword;

        BookIds.Clear();
        BookIds.UnionWith(nextBooks);
        TagIds.Clear();
        TagIds.AddRange(nextTags);
        MatchAnyTag = nextAny;
        SyncSelected(tagsById);
        return true;
    }

    public string Href(string path, IReadOnlyList<BibleBookRow> books)
    {
        var parts = new List<string>();
        var keyword = Keyword.Trim();
        if (keyword.Length > 0)
            parts.Add("q=" + Uri.EscapeDataString(keyword));

        foreach (var book in books)
        {
            if (BookIds.Contains(book.Id))
                parts.Add("books=" + Uri.EscapeDataString(book.Code));
        }

        foreach (var id in TagIds)
            parts.Add("tags=" + id.ToString(CultureInfo.InvariantCulture));

        if (MatchAnyTag)
            parts.Add("op=or");

        return parts.Count == 0 ? path : path + "?" + string.Join("&", parts);
    }

    public bool MatchesLocation(string relative, string path, IReadOnlyList<BibleBookRow> books) =>
        string.Equals(Decode(relative), Decode(Href(path, books)), StringComparison.OrdinalIgnoreCase);

    public void SyncSelected(IReadOnlyDictionary<int, VerseTagRow> tagsById)
    {
        SelectedTags = [];
        foreach (var id in TagIds)
        {
            if (tagsById.TryGetValue(id, out var tag))
                SelectedTags.Add(tag);
        }
    }

    public void RefreshSuggestions(TagCatalog catalog)
    {
        var query = TagQuery.Trim();
        if (query.Length == 0)
        {
            Suggestions = [];
            return;
        }

        Suggestions = catalog.Tags
            .Where(tag => CanSuggest(tag, query, catalog.Index))
            .OrderBy(tag => tag.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(tag => catalog.Index.PathOf(tag.Id), StringComparer.OrdinalIgnoreCase)
            .Take(SuggestionLimit)
            .ToList();
    }

    public void Clear(IReadOnlyDictionary<int, VerseTagRow> tagsById)
    {
        Keyword = "";
        TagQuery = "";
        Suggestions = [];
        BooksOpen = false;
        BookIds.Clear();
        TagIds.Clear();
        MatchAnyTag = false;
        SyncSelected(tagsById);
    }

    bool CanSuggest(VerseTagRow tag, string query, TagIndex index)
    {
        if (TagIds.Contains(tag.Id))
            return false;

        if (HasVerseFilter && index.CountOf(tag.Id) == 0)
            return false;

        return tag.Label.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    static string Decode(string relative)
    {
        relative = relative.Trim();
        if (relative.StartsWith('/'))
            relative = relative[1..];

        var hash = relative.IndexOf('#');
        if (hash >= 0)
            relative = relative[..hash];

        var split = relative.IndexOf('?');
        if (split < 0)
            return relative;

        var path = relative[..split];
        var items = new List<string>();
        foreach (var part in relative[(split + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq < 0)
            {
                items.Add(Uri.UnescapeDataString(part));
                continue;
            }

            items.Add(Uri.UnescapeDataString(part[..eq]) + "=" + Uri.UnescapeDataString(part[(eq + 1)..]));
        }

        return items.Count == 0 ? path : path + "?" + string.Join("&", items);
    }
}
