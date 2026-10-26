namespace BibleVerseExtractor.Services;

[BindSingleton]
public class BibleMetadataService
{
    static readonly FrozenDictionary<string, BibleBookMetadata> BookMetadata = new BibleBookMetadata[]
    {
        Book("GEN", BibleBookLabel.OldTestament, BibleBookLabel.Pentateuch, BibleBookLabel.Torah, BibleBookLabel.Law),
        Book("EXO", BibleBookLabel.OldTestament, BibleBookLabel.Pentateuch, BibleBookLabel.Torah, BibleBookLabel.Law),
        Book("LEV", BibleBookLabel.OldTestament, BibleBookLabel.Pentateuch, BibleBookLabel.Torah, BibleBookLabel.Law),
        Book("NUM", BibleBookLabel.OldTestament, BibleBookLabel.Pentateuch, BibleBookLabel.Torah, BibleBookLabel.Law),
        Book("DEU", BibleBookLabel.OldTestament, BibleBookLabel.Pentateuch, BibleBookLabel.Torah, BibleBookLabel.Law),

        Book("JOS", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.FormerProphets),
        Book("JDG", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.FormerProphets),
        Book("RUT", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.Writings),
        Book("1SA", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.FormerProphets),
        Book("2SA", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.FormerProphets),
        Book("1KI", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.FormerProphets),
        Book("2KI", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.FormerProphets),
        Book("1CH", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.Writings),
        Book("2CH", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.Writings),
        Book("EZR", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.Writings),
        Book("NEH", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.Writings),
        Book("EST", BibleBookLabel.OldTestament, BibleBookLabel.HistoricalBooks, BibleBookLabel.Writings),

        Book("JOB", BibleBookLabel.OldTestament, BibleBookLabel.WisdomBooks, BibleBookLabel.Writings),
        Book("PSA", BibleBookLabel.OldTestament, BibleBookLabel.WisdomBooks, BibleBookLabel.Writings),
        Book("PRO", BibleBookLabel.OldTestament, BibleBookLabel.WisdomBooks, BibleBookLabel.Writings),
        Book("ECC", BibleBookLabel.OldTestament, BibleBookLabel.WisdomBooks, BibleBookLabel.Writings),
        Book("SNG", BibleBookLabel.OldTestament, BibleBookLabel.WisdomBooks, BibleBookLabel.Writings),

        Book("ISA", BibleBookLabel.OldTestament, BibleBookLabel.MajorProphets, BibleBookLabel.LatterProphets),
        Book("JER", BibleBookLabel.OldTestament, BibleBookLabel.MajorProphets, BibleBookLabel.LatterProphets),
        Book("LAM", BibleBookLabel.OldTestament, BibleBookLabel.MajorProphets, BibleBookLabel.Writings),
        Book("EZK", BibleBookLabel.OldTestament, BibleBookLabel.MajorProphets, BibleBookLabel.LatterProphets),
        Book("DAN", BibleBookLabel.OldTestament, BibleBookLabel.MajorProphets, BibleBookLabel.Writings),

        Book("HOS", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("JOL", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("AMO", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("OBA", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("JON", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("MIC", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("NAM", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("HAB", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("ZEP", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("HAG", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("ZEC", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),
        Book("MAL", BibleBookLabel.OldTestament, BibleBookLabel.MinorProphets, BibleBookLabel.LatterProphets),

        Book("MAT", BibleBookLabel.NewTestament, BibleBookLabel.Gospels),
        Book("MRK", BibleBookLabel.NewTestament, BibleBookLabel.Gospels),
        Book("LUK", BibleBookLabel.NewTestament, BibleBookLabel.Gospels),
        Book("JHN", BibleBookLabel.NewTestament, BibleBookLabel.Gospels),
        Book("ACT", BibleBookLabel.NewTestament, BibleBookLabel.History),

        Book("ROM", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("1CO", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("2CO", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("GAL", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("EPH", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("PHP", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("COL", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("1TH", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("2TH", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("1TI", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("2TI", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("TIT", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),
        Book("PHM", BibleBookLabel.NewTestament, BibleBookLabel.PaulineEpistles),

        Book("HEB", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),
        Book("JAS", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),
        Book("1PE", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),
        Book("2PE", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),
        Book("1JN", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),
        Book("2JN", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),
        Book("3JN", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),
        Book("JUD", BibleBookLabel.NewTestament, BibleBookLabel.GeneralEpistles),

        Book("REV", BibleBookLabel.NewTestament, BibleBookLabel.Revelation),
    }.ToFrozenDictionary(book => book.Code);

    static readonly ImmutableArray<BibleOrdering> BookOrders =
    [
        Order("ENG", "English",
            "GEN", "EXO", "LEV", "NUM", "DEU",
            "JOS", "JDG", "RUT", "1SA", "2SA", "1KI", "2KI", "1CH", "2CH", "EZR", "NEH", "EST",
            "JOB", "PSA", "PRO", "ECC", "SNG",
            "ISA", "JER", "LAM", "EZK", "DAN",
            "HOS", "JOL", "AMO", "OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL",
            "MAT", "MRK", "LUK", "JHN", "ACT",
            "ROM", "1CO", "2CO", "GAL", "EPH", "PHP", "COL", "1TH", "2TH", "1TI", "2TI", "TIT", "PHM",
            "HEB", "JAS", "1PE", "2PE", "1JN", "2JN", "3JN", "JUD",
            "REV"),
        Order("HEB", "Hebrew",
            "GEN", "EXO", "LEV", "NUM", "DEU",
            "JOS", "JDG", "1SA", "2SA", "1KI", "2KI",
            "ISA", "JER", "EZK",
            "HOS", "JOL", "AMO", "OBA", "JON", "MIC", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL",
            "PSA", "PRO", "JOB", "SNG", "RUT", "LAM", "ECC", "EST", "DAN", "EZR", "NEH", "1CH", "2CH"),
        Order("GRK", "Greek",
            "GEN", "EXO", "LEV", "NUM", "DEU",
            "JOS", "JDG", "RUT", "1SA", "2SA", "1KI", "2KI", "1CH", "2CH", "EZR", "NEH", "EST",
            "PSA", "PRO", "ECC", "SNG", "JOB",
            "HOS", "AMO", "MIC", "JOL", "OBA", "JON", "NAM", "HAB", "ZEP", "HAG", "ZEC", "MAL",
            "ISA", "JER", "LAM", "EZK", "DAN",
            "MAT", "MRK", "LUK", "JHN", "ACT",
            "ROM", "1CO", "2CO", "GAL", "EPH", "PHP", "COL", "1TH", "2TH", "1TI", "2TI", "TIT", "PHM",
            "HEB", "JAS", "1PE", "2PE", "1JN", "2JN", "3JN", "JUD",
            "REV"),
    ];

    static BibleMetadataService()
    {
        var english = BookOrders.Single(order => order.Code == "ENG");
        if (english.BookCodes.Length != BookMetadata.Count)
            throw new InvalidOperationException("The English book order must include every book.");

        foreach (var order in BookOrders)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var code in order.BookCodes)
            {
                if (!BookMetadata.ContainsKey(code))
                    throw new InvalidOperationException($"Book order '{order.Code}' includes unknown book '{code}'.");

                if (!seen.Add(code))
                    throw new InvalidOperationException($"Book order '{order.Code}' repeats book '{code}'.");
            }
        }
    }

    public BibleBookMetadata GetBook(string code)
    {
        if (!BookMetadata.TryGetValue(code, out var book))
            throw new InvalidOperationException($"No metadata is defined for book '{code}'.");

        return book;
    }

    public IReadOnlyList<BibleOrdering> Orders => BookOrders;

    public static string GetLabelName(BibleBookLabel label) => label switch
    {
        BibleBookLabel.OldTestament => "Old Testament",
        BibleBookLabel.NewTestament => "New Testament",
        BibleBookLabel.Pentateuch => "Pentateuch",
        BibleBookLabel.Torah => "Torah",
        BibleBookLabel.Law => "Law",
        BibleBookLabel.HistoricalBooks => "Historical Books",
        BibleBookLabel.FormerProphets => "Former Prophets",
        BibleBookLabel.WisdomBooks => "Wisdom Books",
        BibleBookLabel.Writings => "Writings",
        BibleBookLabel.MajorProphets => "Major Prophets",
        BibleBookLabel.LatterProphets => "Latter Prophets",
        BibleBookLabel.MinorProphets => "Minor Prophets",
        BibleBookLabel.Gospels => "Gospels",
        BibleBookLabel.History => "History",
        BibleBookLabel.PaulineEpistles => "Pauline Epistles",
        BibleBookLabel.GeneralEpistles => "General Epistles",
        BibleBookLabel.Revelation => "Revelation",
        _ => throw new ArgumentOutOfRangeException(nameof(label), label, "Unknown book label."),
    };

    static BibleBookMetadata Book(string code, params BibleBookLabel[] labels)
        => new(code, [.. labels]);

    static BibleOrdering Order(string code, string name, params string[] bookCodes)
        => new(code, name, [.. bookCodes]);
}

public readonly record struct BibleBookMetadata(
    string Code,
    ImmutableArray<BibleBookLabel> Labels
);

public readonly record struct BibleOrdering(
    string Code,
    string Name,
    ImmutableArray<string> BookCodes
);

public enum BibleBookLabel
{
    OldTestament,
    NewTestament,
    Pentateuch,
    Torah,
    Law,
    HistoricalBooks,
    FormerProphets,
    WisdomBooks,
    MajorProphets,
    MinorProphets,
    LatterProphets,
    Writings,
    Gospels,
    History,
    PaulineEpistles,
    GeneralEpistles,
    Revelation
}
