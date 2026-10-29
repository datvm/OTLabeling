namespace VerseLabeling.Services;

static class ChatClientFactory
{
    public static IChatClient Create(ConfigProvider config, string apiKey)
    {
        var ai = config.AppConfig.Ai ?? throw new LabelingException("Ai is missing.");
        var baseUrl = ai.BaseUrl ?? throw new LabelingException("BaseUrl is missing.");
        var model = ai.Model ?? throw new LabelingException("Model is missing.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new LabelingException("Ai:ApiKey is missing.");

        var client = new OpenAI.Chat.ChatClient(
            model,
            new System.ClientModel.ApiKeyCredential(apiKey),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

        return client.AsIChatClient();
    }
}
