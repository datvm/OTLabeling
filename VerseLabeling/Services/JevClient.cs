using System.Net.Http.Headers;

namespace VerseLabeling.Services;

sealed class JevClient(string apiKey) : IDisposable
{
    public const string BaseUrl = "https://api.typesafe.ai";
    const string Endpoint = "https://api.typesafe.ai/v1/systemone";

    static readonly JsonSerializerOptions RequestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    readonly HttpClient http = new()
    {
        Timeout = TimeSpan.FromMinutes(10),
    };

    public const string LongContextHeader = "TypeSafe-Danger-Zone-Long-Context";

    public async Task<string> AskAsync(string model, TopicRequest topicRequest, bool longContext)
    {
        var requestJson = JsonSerializer.Serialize(
            new JevRequest(model, topicRequest.State, topicRequest.Questions),
            RequestOptions);
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        if (longContext)
            request.Headers.TryAddWithoutValidation(LongContextHeader, "true");

        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new LabelingException("The API call failed: " + Redact(ex.Message));
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var status = (int)response.StatusCode;
            if (status is 429 or 503 or 529)
                throw new JevRetryException(status);

            if (status == 400 && body.Contains("max_tokens_exceeded", StringComparison.Ordinal))
            {
                if (longContext)
                    throw new LabelingException("The request exceeds the long context length.");

                throw new JevContextException();
            }

            if (!response.IsSuccessStatusCode)
                throw new LabelingException("The API call failed: " + Redact(status + " " + Brief(body)));

            if (string.IsNullOrEmpty(body))
                throw new LabelingException("The API call returned no body.");

            return body;
        }
    }

    public void Dispose() => http.Dispose();

    string Redact(string message) =>
        message.Replace(apiKey, "[api key]", StringComparison.Ordinal);

    static string Brief(string body)
    {
        var trimmed = body.Trim();
        if (trimmed.Length <= 500)
            return trimmed;

        return trimmed[..500];
    }

    sealed record JevRequest(string Model, TopicState State, Dictionary<string, NoulQuestion> Questions);
}

sealed class JevRetryException(int status) : Exception
{
    public int Status { get; } = status;
}

sealed class JevContextException : Exception;
