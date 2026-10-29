namespace VerseLabeling.Services;

static class ApiKeyReader
{
    public static string Read()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ApiKeyReader).Assembly, optional: true)
            .Build();

        var key = config["Ai:ApiKey"];
        if (key is null)
            throw new LabelingException("Ai:ApiKey is missing.");

        if (string.IsNullOrWhiteSpace(key))
            throw new LabelingException("Ai:ApiKey is blank.");

        return key;
    }
}
