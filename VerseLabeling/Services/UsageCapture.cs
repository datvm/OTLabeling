namespace VerseLabeling.Services;

sealed class UsageCapture
{
    public RequestUsage? Latest { get; set; }
}

sealed class RequestUsage
{
    public required long InputTokens { get; init; }
    public required long OutputTokens { get; init; }
    public long? ReasoningTokens { get; init; }
    public decimal? Cost { get; init; }
}

sealed class UsageCaptureHandler(UsageCapture capture, string? reasoningEffort) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (reasoningEffort is not null)
            await AddReasoningAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.Content is null || !response.IsSuccessStatusCode)
            return response;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (TryRead(bytes) is RequestUsage usage)
            capture.Latest = usage;

        var copy = new ByteArrayContent(bytes);
        foreach (var header in response.Content.Headers)
            copy.Headers.TryAddWithoutValidation(header.Key, header.Value);

        response.Content = copy;
        return response;
    }

    static RequestUsage? TryRead(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!document.RootElement.TryGetProperty("usage", out var usage) ||
                usage.ValueKind != JsonValueKind.Object)
                return null;

            if (!TryToken(usage, "prompt_tokens", out var input) ||
                !TryToken(usage, "completion_tokens", out var output))
                return null;

            decimal? cost = null;
            if (usage.TryGetProperty("cost", out var costValue) &&
                costValue.ValueKind == JsonValueKind.Number &&
                costValue.TryGetDecimal(out var amount))
                cost = amount;

            long? reasoning = null;
            if (usage.TryGetProperty("completion_tokens_details", out var details) &&
                details.ValueKind == JsonValueKind.Object &&
                TryToken(details, "reasoning_tokens", out var reasoningTokens))
                reasoning = reasoningTokens;

            return new RequestUsage
            {
                InputTokens = input,
                OutputTokens = output,
                ReasoningTokens = reasoning,
                Cost = cost,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    async Task AddReasoningAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is null)
            return;

        var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        System.Text.Json.Nodes.JsonNode? node;
        try
        {
            node = System.Text.Json.Nodes.JsonNode.Parse(bytes);
        }
        catch (JsonException)
        {
            Replace(request, bytes);
            return;
        }

        if (node is not System.Text.Json.Nodes.JsonObject body)
        {
            Replace(request, bytes);
            return;
        }

        body["reasoning"] = new System.Text.Json.Nodes.JsonObject
        {
            ["effort"] = reasoningEffort,
        };
        Replace(request, JsonSerializer.SerializeToUtf8Bytes(body));
    }

    static void Replace(HttpRequestMessage request, byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;

                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        request.Content = content;
    }

    static bool TryToken(JsonElement usage, string name, out long value)
    {
        value = 0;
        return usage.TryGetProperty(name, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt64(out value);
    }
}
