namespace WebClient.Data;

public sealed class BibleBookRow
{
    public const string OldTestament = "Old Testament";
    public const string NewTestament = "New Testament";

    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Testament { get; set; } = "";
    public List<string> Labels { get; set; } = [];
}

public sealed class BookOrderRow
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public sealed class BibleChapterRow
{
    public int Chapter { get; set; }
}

public sealed class BibleVerseRow
{
    public int Chapter { get; set; }
    public int VerseNumber { get; set; }
    public string Text { get; set; } = "";
    public int AccumulatedVerseNumber { get; set; }
    public List<List<VerseTagRef>> Tags { get; set; } = [];
}

static class TagText
{
    public static bool HideRoot(string? subclassifier) =>
        string.IsNullOrEmpty(subclassifier)
        || subclassifier is "People" or "God" or "Places"
        || subclassifier.Contains('.');
}

public sealed class VerseTagRef
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Subclassifier { get; set; } = "";
    public string Label => TagText.HideRoot(Subclassifier) ? Name : $"{Name} ({Subclassifier})";
}

public sealed class VerseTagParentLink
{
    public int TagId { get; set; }
    public int ParentTagId { get; set; }
}

public sealed class VerseTagRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Subclassifier { get; set; } = "";
    public int VerseCount { get; set; }
    public string Label => TagText.HideRoot(Subclassifier) ? Name : $"{Name} ({Subclassifier})";
}

public sealed class TaggedVerseRow
{
    public int BookId { get; set; }
    public string BookCode { get; set; } = "";
    public string BookName { get; set; } = "";
    public int Chapter { get; set; }
    public int VerseNumber { get; set; }
    public string Text { get; set; } = "";
    public List<List<VerseTagRef>> Tags { get; set; } = [];
}
