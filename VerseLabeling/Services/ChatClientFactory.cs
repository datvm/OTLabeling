namespace VerseLabeling.Services;

static class ChatClientFactory
{
    public static IChatClient Create(ConfigProvider config, string apiKey, UsageCapture usage)
    {
        var ai = config.AppConfig.Ai ?? throw new LabelingException("Ai is missing.");
        var baseUrl = ai.BaseUrl ?? throw new LabelingException("BaseUrl is missing.");
        var model = ai.Model ?? throw new LabelingException("Model is missing.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new LabelingException("Ai:ApiKey is missing.");

        var effort = RunSettings.ReadReasoning(ai.ReasoningEffort);
        var http = new HttpClient(new UsageCaptureHandler(usage, effort) { InnerHandler = new HttpClientHandler() })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        var client = new OpenAI.Chat.ChatClient(
            model,
            new System.ClientModel.ApiKeyCredential(apiKey),
            new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(baseUrl),
                Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(http),
            });

        return client.AsIChatClient();
    }
}
