namespace Common.Entities;

[Index(nameof(Name), nameof(Subclassifier), IsUnique = true)]
public class VerseTag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Subclassifier { get; set; } = "";
}

[Index(nameof(TagId), nameof(ParentTagId), IsUnique = true)]
[Index(nameof(ParentTagId))]
public class VerseTagParent
{
    public int Id { get; set; }
    public int TagId { get; set; }
    public int ParentTagId { get; set; }
}

[Index(nameof(VerseId), nameof(VerseTagId), IsUnique = true)]
[Index(nameof(VerseId), nameof(TagIndex), IsUnique = true)]
[Index(nameof(VerseTagId))]
public class VerseTagMapping
{
    public int Id { get; set; }
    public int VerseId { get; set; }
    public int VerseTagId { get; set; }
    public int TagIndex { get; set; }
}

[Index(nameof(BookId), nameof(Chapter), IsUnique = true)]
public class ChapterContext
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int Chapter { get; set; }
    public string SummarySoFar { get; set; } = "";
    public string ImmediateContext { get; set; } = "";
}

[Index(nameof(LabelRunId), nameof(SortOrder), IsUnique = true)]
[Index(nameof(LabelRunId), nameof(BookId), nameof(Chapter), IsUnique = true)]
public class LabelRunChapter
{
    public int Id { get; set; }
    public int LabelRunId { get; set; }
    public int BookId { get; set; }
    public int Chapter { get; set; }
    public int SortOrder { get; set; }
    public bool Filled { get; set; }
    public string SummarySoFar { get; set; } = "";
    public string ImmediateContext { get; set; } = "";
}

[Index(nameof(RunKey), IsUnique = true)]
public class LabelRun
{
    public const string StatusRunning = "Running";
    public const string StatusCompleted = "Completed";
    public const string StatusFailed = "Failed";

    public int Id { get; set; }
    public required string RunKey { get; set; }
    public required string Step { get; set; }
    public required string Books { get; set; }
    public required string Chapters { get; set; }
    public required string Summary { get; set; }
    public required string ImmediateContext { get; set; }
    public int Rpm { get; set; }
    public required string BaseUrl { get; set; }
    public required string Model { get; set; }
    public required string Status { get; set; }
}

[Index(nameof(LabelRunId), nameof(VerseId), nameof(TagIndex), IsUnique = true)]
[Index(nameof(LabelRunId), nameof(VerseId), nameof(VerseTagId), IsUnique = true)]
[Index(nameof(VerseId))]
[Index(nameof(VerseTagId))]
public class LabelRunTag
{
    public int Id { get; set; }
    public int LabelRunId { get; set; }
    public int VerseId { get; set; }
    public int VerseTagId { get; set; }
    public int TagIndex { get; set; }
}

[Index(nameof(Code), IsUnique = true)]
public class Topic
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public required string Question { get; set; }
}

[Index(nameof(LabelRunId), nameof(SortOrder), IsUnique = true)]
[Index(nameof(LabelRunId), nameof(Code), IsUnique = true)]
public class LabelRunTopic
{
    public int Id { get; set; }
    public int LabelRunId { get; set; }
    public int SortOrder { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public required string Question { get; set; }
}

[Index(nameof(LabelRunId), nameof(VerseId), nameof(LabelRunTopicId), IsUnique = true)]
[Index(nameof(VerseId))]
[Index(nameof(LabelRunTopicId))]
public class LabelRunTopicAnswer
{
    public const string Yes = "Yes";
    public const string No = "No";
    public const string Ambiguous = "Ambiguous";

    public int Id { get; set; }
    public int LabelRunId { get; set; }
    public int VerseId { get; set; }
    public int LabelRunTopicId { get; set; }
    public required string Answer { get; set; }
    public decimal Probability { get; set; }
}

[Index(nameof(VerseId), nameof(Code), IsUnique = true)]
[Index(nameof(Code))]
[Index(nameof(LabelRunId))]
public class VerseTopicAnswer
{
    public int Id { get; set; }
    public int VerseId { get; set; }
    public required string Code { get; set; }
    public required string Answer { get; set; }
    public decimal Probability { get; set; }
    public int LabelRunId { get; set; }
}

[Index(nameof(VerseId), IsUnique = true)]
public class VerseTagResolution
{
    public int Id { get; set; }
    public int VerseId { get; set; }
    public int RunAId { get; set; }
    public int RunBId { get; set; }
    public int ChosenRunId { get; set; }
}
