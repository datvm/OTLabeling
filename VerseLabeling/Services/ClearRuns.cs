namespace VerseLabeling.Services;

static class ClearRuns
{
    public const string StepName = "clear-runs";

    static readonly string[] Tables =
    [
        "VerseTopicAnswers",
        "LabelRunTopicAnswers",
        "LabelRunTopics",
        "VerseTagResolutions",
        "LabelRunTags",
        "VerseTagMappings",
        "VerseTagParents",
        "VerseTags",
        "LabelRunChapters",
        "LabelRuns",
        "ChapterContexts",
    ];

    public static async Task RunAsync(LabelingContext database, ConfigProvider config)
    {
        var existing = await TableNamesAsync(database);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        await using var transaction = await database.Database.BeginTransactionAsync();
        foreach (var table in Tables)
        {
            if (!existing.Contains(table))
                continue;

            counts[table] = await DeleteAsync(database, table);
        }

        await transaction.CommitAsync();

        var runs = counts.GetValueOrDefault("LabelRuns");
        var tags = counts.GetValueOrDefault("VerseTags");
        if (runs == 0 && tags == 0)
            Console.WriteLine("There were no label runs to clear.");
        else
            Console.WriteLine($"Cleared {runs} label runs and {tags} verse tags.");

        var logs = Path.Combine(config.AppConfigPath, "out", "verse-labeling");
        if (!Directory.Exists(logs))
        {
            Console.WriteLine("There were no run logs to clear.");
            return;
        }

        Directory.Delete(logs, recursive: true);
        Console.WriteLine("Cleared the run logs.");
    }

    static Task<int> DeleteAsync(LabelingContext database, string table) => table switch
    {
        "VerseTopicAnswers" => database.Database.ExecuteSqlAsync($"DELETE FROM \"VerseTopicAnswers\";"),
        "LabelRunTopicAnswers" => database.Database.ExecuteSqlAsync($"DELETE FROM \"LabelRunTopicAnswers\";"),
        "LabelRunTopics" => database.Database.ExecuteSqlAsync($"DELETE FROM \"LabelRunTopics\";"),
        "VerseTagResolutions" => database.Database.ExecuteSqlAsync($"DELETE FROM \"VerseTagResolutions\";"),
        "LabelRunTags" => database.Database.ExecuteSqlAsync($"DELETE FROM \"LabelRunTags\";"),
        "VerseTagMappings" => database.Database.ExecuteSqlAsync($"DELETE FROM \"VerseTagMappings\";"),
        "VerseTagParents" => database.Database.ExecuteSqlAsync($"DELETE FROM \"VerseTagParents\";"),
        "VerseTags" => database.Database.ExecuteSqlAsync($"DELETE FROM \"VerseTags\";"),
        "LabelRunChapters" => database.Database.ExecuteSqlAsync($"DELETE FROM \"LabelRunChapters\";"),
        "LabelRuns" => database.Database.ExecuteSqlAsync($"DELETE FROM \"LabelRuns\";"),
        "ChapterContexts" => database.Database.ExecuteSqlAsync($"DELETE FROM \"ChapterContexts\";"),
        _ => throw new LabelingException($"Table '{table}' is not a labeling table."),
    };

    static async Task<HashSet<string>> TableNamesAsync(LabelingContext database)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        await database.Database.OpenConnectionAsync();
        try
        {
            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                names.Add(reader.GetString(0));
        }
        finally
        {
            await database.Database.CloseConnectionAsync();
        }

        return names;
    }
}
