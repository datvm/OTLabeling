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
    public string? AppliedEffort { get; set; }
}

sealed class UsageCaptureHandler(UsageCapture capture, string? reasoningEffort) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? appliedEffort = null;
        if (reasoningEffort is not null)
            appliedEffort = await AddReasoningAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.Content is null || !response.IsSuccessStatusCode)
            return response;

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (TryRead(bytes) is RequestUsage usage)
        {
            usage.AppliedEffort = appliedEffort;
            capture.Latest = usage;
        }

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

    async Task<string> AddReasoningAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is null)
            throw new LabelingException("The request has no body, so ReasoningEffort was not sent.");

        var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        System.Text.Json.Nodes.JsonNode? node;
        try
        {
            node = System.Text.Json.Nodes.JsonNode.Parse(bytes);
        }
        catch (JsonException)
        {
            Replace(request, bytes);
            throw new LabelingException("The request body is not JSON, so ReasoningEffort was not sent.");
        }

        if (node is not System.Text.Json.Nodes.JsonObject body)
        {
            Replace(request, bytes);
            throw new LabelingException("The request body is not a JSON object, so ReasoningEffort was not sent.");
        }

        body["reasoning"] = new System.Text.Json.Nodes.JsonObject
        {
            ["effort"] = reasoningEffort,
        };
        var written = JsonSerializer.SerializeToUtf8Bytes(body);
        Replace(request, written);
        if (!WrittenEffortIs(written, reasoningEffort!))
            throw new LabelingException("ReasoningEffort was not written on the request.");

        return reasoningEffort!;
    }

    static bool WrittenEffortIs(byte[] bytes, string effort)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            return document.RootElement.TryGetProperty("reasoning", out var reasoning) &&
                reasoning.ValueKind == JsonValueKind.Object &&
                reasoning.TryGetProperty("effort", out var value) &&
                value.ValueKind == JsonValueKind.String &&
                value.GetString() == effort;
        }
        catch (JsonException)
        {
            return false;
        }
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
