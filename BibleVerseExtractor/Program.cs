#if DEBUG
args = ["--metadata-only"];
#endif

if (args.Any(arg => arg != "--metadata-only"))
    throw new InvalidOperationException("Usage: BibleVerseExtractor [--metadata-only]");

var root = await CommonUtils.CreateServiceProviderAsync(s => s.AddServiceSharp());

await using var scope = root.CreateAsyncScope();
var services = scope.ServiceProvider;
var filler = services.GetRequiredService<DbFillingService>();

if (args.Contains("--metadata-only"))
    await filler.UpdateMetadataAsync();
else
    await filler.FillDbAsync();