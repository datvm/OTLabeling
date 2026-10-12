namespace Common.Entities;

public class LabelingContext(DbContextOptions<LabelingContext> options) : DbContext(options)
{
    public DbSet<BibleBook> BibleBooks { get; set; }
    public DbSet<BibleVerse> BibleVerses { get; set; }

    public async Task InitializeAsync()
    {
        await Database.MigrateAsync();
    }

    public static string GetDbPath(ConfigProvider configProvider)
    {
        var path = Path.GetFullPath(Path.Combine(configProvider.AppConfigPath, configProvider.AppConfig.DbPath));

        // Ensure the folder is created
        var folder = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(folder);

        return path;
    }

    public static void Configure(DbContextOptionsBuilder options, ConfigProvider configProvider)
    {
        var connString = $"Data Source={GetDbPath(configProvider)}";

        options
            .UseSqlite(connString, opts => opts
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

}
