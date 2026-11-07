namespace WebClient.Data;

public sealed class TagIndex
{
    static readonly int[] NoIds = [];

    readonly Dictionary<int, VerseTagRow> byId = [];
    readonly Dictionary<int, HashSet<int>> subtreeVerses = [];
    readonly Dictionary<int, int> verseBook = [];
    readonly Dictionary<int, List<int>> parentsByChild = [];
    readonly Dictionary<int, List<int>> childrenByParent = [];
    readonly Dictionary<int, string> paths = [];
    readonly HashSet<int> allVerses = [];
    readonly Dictionary<int, int> counts = [];

    TagIndex(
        IReadOnlyList<VerseTagRow> tags,
        IReadOnlyList<VerseTagParentLink> links,
        IReadOnlyList<TagVerseLink> occurrences)
    {
        foreach (var tag in tags)
        {
            byId[tag.Id] = tag;
            subtreeVerses[tag.Id] = [];
        }

        foreach (var link in links)
        {
            if (!byId.ContainsKey(link.TagId) || !byId.ContainsKey(link.ParentTagId))
                continue;

            Add(parentsByChild, link.TagId, link.ParentTagId);
            Add(childrenByParent, link.ParentTagId, link.TagId);
        }

        foreach (var link in occurrences)
        {
            if (!subtreeVerses.ContainsKey(link.TagId))
                continue;

            allVerses.Add(link.VerseId);
            verseBook[link.VerseId] = link.BookId;
            AddVerse(link.TagId, link.VerseId);
        }

        var trail = new HashSet<int>();
        foreach (var tag in tags)
            PathFor(tag.Id, trail);
    }

    public int MatchedVerseCount { get; private set; }

    public static TagIndex Create(
        IReadOnlyList<VerseTagRow> tags,
        IReadOnlyList<VerseTagParentLink> links,
        IReadOnlyList<TagVerseLink> occurrences)
        => new(tags, links, occurrences);

    public int CountOf(int id) => counts[id];

    public string PathOf(int id) => paths[id];

    public IReadOnlyList<int> ChildrenOf(int id) =>
        childrenByParent.TryGetValue(id, out var children) ? children : NoIds;

    public IReadOnlyList<int> ParentsOf(int id) =>
        parentsByChild.TryGetValue(id, out var parents) ? parents : NoIds;

    public bool Covers(int ancestorId, int tagId)
    {
        var stack = new Stack<int>();
        var seen = new HashSet<int>();
        stack.Push(tagId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!seen.Add(id))
                continue;
            if (id == ancestorId)
                return true;

            foreach (var parent in ParentsOf(id))
                stack.Push(parent);
        }

        return false;
    }

    public void Apply(IReadOnlySet<int> bookIds, IReadOnlyList<int> tagIds, bool matchAnyTag)
    {
        HashSet<int>? matched = null;
        if (bookIds.Count > 0 || tagIds.Count > 0)
            matched = Match(bookIds, tagIds, matchAnyTag);

        MatchedVerseCount = matched?.Count ?? allVerses.Count;
        counts.Clear();
        foreach (var id in subtreeVerses.Keys)
            counts[id] = Count(id, matched);
    }

    HashSet<int> Match(IReadOnlySet<int> bookIds, IReadOnlyList<int> tagIds, bool matchAnyTag)
    {
        var result = new HashSet<int>();
        if (matchAnyTag && tagIds.Count > 0)
        {
            foreach (var tagId in tagIds)
            {
                foreach (var verseId in subtreeVerses[tagId])
                {
                    if (bookIds.Count > 0 && !BookMatches(verseId, bookIds))
                        continue;

                    result.Add(verseId);
                }
            }

            return result;
        }

        IEnumerable<int> source = tagIds.Count == 0 ? allVerses : subtreeVerses[tagIds[0]];
        foreach (var verseId in source)
        {
            if (bookIds.Count > 0 && !BookMatches(verseId, bookIds))
                continue;

            var include = true;
            for (var index = 1; index < tagIds.Count; index++)
            {
                if (!subtreeVerses[tagIds[index]].Contains(verseId))
                {
                    include = false;
                    break;
                }
            }

            if (include)
                result.Add(verseId);
        }

        return result;
    }

    bool BookMatches(int verseId, IReadOnlySet<int> bookIds) =>
        verseBook.TryGetValue(verseId, out var bookId) && bookIds.Contains(bookId);

    int Count(int tagId, HashSet<int>? matched)
    {
        var verses = subtreeVerses[tagId];
        if (matched is null)
            return verses.Count;

        var count = 0;
        if (matched.Count < verses.Count)
        {
            foreach (var verseId in matched)
            {
                if (verses.Contains(verseId))
                    count++;
            }
        }
        else
        {
            foreach (var verseId in verses)
            {
                if (matched.Contains(verseId))
                    count++;
            }
        }

        return count;
    }

    void AddVerse(int tagId, int verseId)
    {
        var stack = new Stack<int>();
        stack.Push(tagId);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!subtreeVerses.TryGetValue(id, out var verses) || !verses.Add(verseId))
                continue;

            if (!parentsByChild.TryGetValue(id, out var parents))
                continue;

            foreach (var parent in parents)
                stack.Push(parent);
        }
    }

    string PathFor(int id, HashSet<int> trail)
    {
        if (paths.TryGetValue(id, out var ready))
            return ready;

        var label = byId[id].Label;
        if (!trail.Add(id))
            return label;

        try
        {
            if (!parentsByChild.TryGetValue(id, out var parents) || parents.Count == 0)
            {
                paths[id] = label;
                return label;
            }

            var prefix = new List<string>();
            foreach (var parentId in parents)
                prefix.Add(PathFor(parentId, trail));

            var text = string.Join(" / ", prefix.Distinct(StringComparer.Ordinal)) + " › " + label;
            paths[id] = text;
            return text;
        }
        finally
        {
            trail.Remove(id);
        }
    }

    static void Add(Dictionary<int, List<int>> map, int key, int value)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map.Add(key, list);
        }

        list.Add(value);
    }
}
