namespace Common.Entities;

public class DesignTimeContextFactory : IDesignTimeDbContextFactory<LabelingContext>
{
    public LabelingContext CreateDbContext(string[] args)
    {
        var configProvider = new ConfigProvider();
        configProvider.LoadAsync().GetAwaiter().GetResult();

        var options = new DbContextOptionsBuilder<LabelingContext>();
        LabelingContext.Configure(options, configProvider);
        return new LabelingContext(options.Options);
    }
}
