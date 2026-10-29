namespace Common.Entities;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(ParentId))]
public class VerseTag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int? ParentId { get; set; }
    public VerseTag? Parent { get; set; }
    public ICollection<VerseTag> Children { get; set; } = [];
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

[Index(nameof(VerseId), IsUnique = true)]
public class VerseTagResolution
{
    public int Id { get; set; }
    public int VerseId { get; set; }
    public int RunAId { get; set; }
    public int RunBId { get; set; }
    public int ChosenRunId { get; set; }
}
