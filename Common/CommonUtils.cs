namespace Common;

public static class CommonUtils
{

    public static async Task<IServiceProvider> CreateServiceProviderAsync(Action<IServiceCollection>? extras = null)
    {
        var col = new ServiceCollection();
        col.AddLabelingCommon();
        extras?.Invoke(col);

        var services = col.BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<ConfigProvider>().LoadAsync();        
        await sp.GetRequiredService<LabelingContext>().InitializeAsync();

        return services;
    }

}
