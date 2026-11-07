namespace VerseLabeling.Services;

static class ApiKeyReader
{
    public static string Read() => ReadSecret("Ai:ApiKey");

    public static string ReadJev() => ReadSecret("Jev:ApiKey");

    static string ReadSecret(string name)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ApiKeyReader).Assembly, optional: true)
            .Build();

        var key = config[name];
        if (key is null)
            throw new LabelingException($"{name} is missing.");

        if (string.IsNullOrWhiteSpace(key))
            throw new LabelingException($"{name} is blank.");

        return key;
    }
}
