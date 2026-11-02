if (args.Length > 0)
{
    Console.WriteLine("A process argument is not allowed.");
    Environment.ExitCode = 1;
    return;
}

await RunAsync();

static async Task RunAsync()
{
    try
    {
        string? apiKey = null;
        await using var provider = (ServiceProvider)await CommonUtils.CreateServiceProviderAsync(services =>
            services.AddSingleton<IChatClient>(sp =>
                ChatClientFactory.Create(sp.GetRequiredService<ConfigProvider>(), apiKey!)));

        var config = provider.GetRequiredService<ConfigProvider>();
        var step = config.AppConfig.VerseLabeling?.Step?.Trim();
        if (step == ClearRuns.StepName)
        {
            await using var clearScope = provider.CreateAsyncScope();
            var clearDatabase = clearScope.ServiceProvider.GetRequiredService<LabelingContext>();
            await ClearRuns.RunAsync(clearDatabase, config);
            return;
        }

        var settings = RunSettings.Read(config.AppConfig);
        apiKey = ApiKeyReader.Read();

        await using var scope = provider.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<LabelingContext>();
        var chat = scope.ServiceProvider.GetRequiredService<IChatClient>();
        var labeler = new IsraeliteLabeler(database, config, chat, apiKey);
        await labeler.RunAsync(settings);
    }
    catch (LabelingException ex)
    {
        Console.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
}
