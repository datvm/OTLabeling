if (args.Length > 0)
{
    Console.WriteLine("A process argument is not allowed.");
    Environment.ExitCode = 1;
    return;
}

try
{
    await RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

static async Task RunAsync()
{
    var services = new ServiceCollection();
    services.AddLabelingCommon();
    string? apiKey = null;
    services.AddSingleton<IChatClient>(sp =>
        ChatClientFactory.Create(sp.GetRequiredService<ConfigProvider>(), apiKey!));

    await using var provider = services.BuildServiceProvider();
    var config = provider.GetRequiredService<ConfigProvider>();
    await config.LoadAsync();

    var settings = RunSettings.Read(config.AppConfig);
    apiKey = ApiKeyReader.Read();

    await using var scope = provider.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<LabelingContext>();
    var chat = scope.ServiceProvider.GetRequiredService<IChatClient>();
    var labeler = new IsraeliteLabeler(database, config, chat, apiKey);
    await labeler.RunAsync(settings);
}
