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
}
