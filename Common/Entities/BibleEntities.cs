namespace Common.Entities;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Code), IsUnique = true)]
public class BibleBook
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    public ICollection<BibleVerse> Verses { get; set; } = [];
    public ICollection<BookLabelMapping> LabelMappings { get; set; } = [];
    public ICollection<BookOrderItem> OrderItems { get; set; } = [];
}

[Index(nameof(Name), IsUnique = true)]
public class BookLabel
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<BookLabelMapping> BookMappings { get; set; } = [];
}

[Index(nameof(BookId), nameof(BookLabelId), IsUnique = true)]
public class BookLabelMapping
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public BibleBook? Book { get; set; }

    public int BookLabelId { get; set; }
    public BookLabel? BookLabel { get; set; }
}

[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Name), IsUnique = true)]
public class BookOrder
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    public ICollection<BookOrderItem> Items { get; set; } = [];
}

[Index(nameof(BookOrderId), nameof(BookId), nameof(Order), IsUnique = true)]
public class BookOrderItem
{
    public int Id { get; set; }

    public int BookOrderId { get; set; }
    public BookOrder? BookOrder { get; set; }

    public int BookId { get; set; }
    public BibleBook? Book { get; set; }

    public int Order { get; set; }
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
