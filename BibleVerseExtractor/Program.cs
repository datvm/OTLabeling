var root = await CommonUtils.CreateServiceProviderAsync(s => s.AddServiceSharp());

await using var scope = root.CreateAsyncScope();
var services = scope.ServiceProvider;

await services.GetRequiredService<DbFillingService>().FillDbAsync();