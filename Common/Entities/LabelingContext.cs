namespace Common.Entities;

public class LabelingContext(DbContextOptions<LabelingContext> options) : DbContext(options)
{
    public DbSet<BibleBook> BibleBooks { get; set; }
    public DbSet<BibleVerse> BibleVerses { get; set; }
    public DbSet<BookLabel> BookLabels { get; set; }
    public DbSet<BookLabelMapping> BookLabelMappings { get; set; }
    public DbSet<BookOrder> BookOrders { get; set; }
    public DbSet<BookOrderItem> BookOrderItems { get; set; }
    public DbSet<VerseTag> VerseTags { get; set; }
    public DbSet<VerseTagMapping> VerseTagMappings { get; set; }
    public DbSet<LabelRun> LabelRuns { get; set; }
    public DbSet<LabelRunTag> LabelRunTags { get; set; }
    public DbSet<VerseTagResolution> VerseTagResolutions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VerseTag>(entity =>
        {
            entity.Property(tag => tag.Name).UseCollation("BINARY");
            entity.HasOne(tag => tag.Parent)
                .WithMany(tag => tag.Children)
                .HasForeignKey(tag => tag.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VerseTagMapping>(entity =>
        {
            entity.HasOne<BibleVerse>()
                .WithMany()
                .HasForeignKey(mapping => mapping.VerseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<VerseTag>()
                .WithMany()
                .HasForeignKey(mapping => mapping.VerseTagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LabelRunTag>(entity =>
        {
            entity.HasOne<LabelRun>()
                .WithMany()
                .HasForeignKey(tag => tag.LabelRunId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BibleVerse>()
                .WithMany()
                .HasForeignKey(tag => tag.VerseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<VerseTag>()
                .WithMany()
                .HasForeignKey(tag => tag.VerseTagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VerseTagResolution>(entity =>
        {
            entity.HasOne<BibleVerse>()
                .WithMany()
                .HasForeignKey(resolution => resolution.VerseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LabelRun>()
                .WithMany()
                .HasForeignKey(resolution => resolution.RunAId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LabelRun>()
                .WithMany()
                .HasForeignKey(resolution => resolution.RunBId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LabelRun>()
                .WithMany()
                .HasForeignKey(resolution => resolution.ChosenRunId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_VerseTagResolution_Choice",
                "[RunAId] <> [RunBId] AND ([ChosenRunId] = [RunAId] OR [ChosenRunId] = [RunBId])"));
        });
    }

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
