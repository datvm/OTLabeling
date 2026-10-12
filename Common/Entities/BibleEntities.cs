namespace Common.Entities;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Code), IsUnique = true)]
public class BibleBook
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    public ICollection<BibleVerse> Verses { get; set; } = [];
}

[Index(nameof(BookId), nameof(Chapter), nameof(VerseNumber), IsUnique = true)]
[Index(nameof(BookId), nameof(AccumulatedVerseNumber), IsUnique = true)]
public class BibleVerse
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public BibleBook? Book { get; set; }

    public int Chapter { get; set; }
    public int VerseNumber { get; set; }
    public required string Text { get; set; }    
    
    public int AccumulatedVerseNumber { get; set; }
}
