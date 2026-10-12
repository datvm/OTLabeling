namespace Common;

[BindSingleton]
public class ConfigProvider
{
    const string ConfigFileName = "project-conf.json";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    AppConfig? appConfig;
    string? appConfigPath;

    public AppConfig AppConfig => appConfig ?? throw new InvalidOperationException("AppConfig is not loaded yet.");
    public string AppConfigPath => appConfigPath ?? throw new InvalidOperationException("AppConfig is not loaded yet.");

    public async Task LoadAsync()
    {
        var configDirectory = FindConfigDirectory();
        var configFile = Path.Combine(configDirectory, ConfigFileName);

        await using var stream = File.OpenRead(configFile);
        var config = await JsonSerializer.DeserializeAsync<AppConfig>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Could not read configuration from '{configFile}'.");

        if (string.IsNullOrWhiteSpace(config.DbPath))
            throw new InvalidOperationException($"'{configFile}' must set DbPath.");

        appConfig = config;
        appConfigPath = configDirectory;
    }

    static string FindConfigDirectory()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(Path.GetFullPath(start));
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, ConfigFileName)))
                {
                    return dir.FullName;
                }   

                dir = dir.Parent;
            }
        }

        throw new FileNotFoundException(
            $"Could not find '{ConfigFileName}' in the working directory, the application directory, or any parent folder.");
    }

}

public record AppConfig(
    string DbPath
);