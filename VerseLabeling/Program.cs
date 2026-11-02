if (args.Length > 0)
{
    Console.WriteLine("A process argument is not allowed.");
    Environment.ExitCode = 1;
    return;
}

await RunAsync();

static async Task RunAsync()
{
    string? apiKey = null;
    await using var provider = (ServiceProvider)await CommonUtils.CreateServiceProviderAsync(services =>
        services.AddSingleton<IChatClient>(sp =>
            ChatClientFactory.Create(sp.GetRequiredService<ConfigProvider>(), apiKey!)));

    var config = provider.GetRequiredService<ConfigProvider>();
    var settings = RunSettings.Read(config.AppConfig);
    apiKey = ApiKeyReader.Read();

    await using var scope = provider.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<LabelingContext>();
    var chat = scope.ServiceProvider.GetRequiredService<IChatClient>();
    var labeler = new IsraeliteLabeler(database, config, chat, apiKey);
    await labeler.RunAsync(settings);
}
