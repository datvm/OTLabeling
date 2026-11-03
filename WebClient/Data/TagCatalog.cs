namespace WebClient.Data;

public sealed class TagCatalog(LabelingDatabase database)
{
    public IReadOnlyList<VerseTagRow> Tags { get; private set; } = [];
    public IReadOnlyList<BibleBookRow> Books { get; private set; } = [];
    public IReadOnlyList<VerseTagParentLink> Links { get; private set; } = [];
    public IReadOnlyDictionary<int, VerseTagRow> TagsById { get; private set; } = new Dictionary<int, VerseTagRow>();
    public IReadOnlyDictionary<string, int> LabelCounts { get; private set; } = new Dictionary<string, int>();
    public TagIndex Index { get; private set; } = null!;
    public bool Loaded { get; private set; }

    public async Task LoadAsync()
    {
        if (Loaded)
            return;

        var tags = (await database.GetVerseTagsAsync()).ToList();
        var links = (await database.GetVerseTagParentLinksAsync()).ToList();
        var books = (await database.GetBooksAsync(LabelingDatabase.DefaultBookOrderCode)).ToList();
        var occurrences = await database.GetTagVerseLinksAsync();
        Tags = tags;
        Links = links;
        Books = books;
        TagsById = tags.ToDictionary(tag => tag.Id);
        LabelCounts = tags
            .GroupBy(tag => tag.Label, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        Index = TagIndex.Create(tags, links, occurrences);
        Loaded = true;
    }

    public void Apply(IReadOnlySet<int> bookIds, IReadOnlyList<int> tagIds)
    {
        Index.Apply(bookIds, tagIds);
        foreach (var tag in Tags)
            tag.VerseCount = Index.CountOf(tag.Id);
    }

    public string ChipText(VerseTagRow tag) =>
        LabelCounts[tag.Label] > 1 ? Index.PathOf(tag.Id) : tag.Label;

    public int MatchedVerseCount => Index.MatchedVerseCount;
}
