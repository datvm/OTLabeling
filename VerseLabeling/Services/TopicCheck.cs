namespace VerseLabeling.Services;

static class TopicCheck
{
    public const decimal YesAt = 0.7m;
    public const decimal NoAt = 0.3m;

    static readonly JsonSerializerOptions ResponseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static (long? Input, long? Output) ReadUsage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return (null, null);

            if (!document.RootElement.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
                return (null, null);

            long? input = usage.TryGetProperty("input_tokens", out var inputValue) && inputValue.TryGetInt64(out var inputTokens)
                ? inputTokens
                : null;
            long? output = usage.TryGetProperty("output_tokens", out var outputValue) && outputValue.TryGetInt64(out var outputTokens)
                ? outputTokens
                : null;
            return (input, output);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    public static JevResponse Parse(string body)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            throw new LabelingException("The body is not one JSON object.");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new LabelingException("The body is not one JSON object.");

            try
            {
                return document.RootElement.Deserialize<JevResponse>(ResponseOptions)
                    ?? throw new LabelingException("The body is not one JSON object.");
            }
            catch (JsonException)
            {
                throw new LabelingException("A JSON value has the wrong type.");
            }
        }
    }

    public static TopicChecked Check(
        JevResponse response,
        IReadOnlyList<BibleVerse> verses,
        IReadOnlyList<RunTopic> topics)
    {
        var answers = response.Answers ?? throw new LabelingException("A JSON value has the wrong type.");
        var expected = new Dictionary<string, (BibleVerse Verse, RunTopic Topic)>(StringComparer.Ordinal);
        foreach (var verse in verses)
        {
            foreach (var topic in topics)
                expected[TopicPrompt.Key(verse.VerseNumber, topic.SortOrder)] = (verse, topic);
        }

        var errors = new List<string>();
        foreach (var key in answers.Keys)
        {
            if (!expected.ContainsKey(key))
                errors.Add($"Answer '{key}' is not a question from this request.");
        }

        var verdicts = new List<TopicVerdict>();
        foreach (var pair in expected)
        {
            var verseNumber = pair.Value.Verse.VerseNumber;
            var code = pair.Value.Topic.Code;
            if (!answers.TryGetValue(pair.Key, out var answer))
            {
                errors.Add($"Verse {verseNumber} topic {code} is absent.");
                continue;
            }

            if (!string.Equals(answer.Type, "noul", StringComparison.Ordinal) || answer.Noul is not decimal probability)
            {
                errors.Add($"Verse {verseNumber} topic {code} is not a yes/no answer.");
                continue;
            }

            if (probability < 0 || probability > 1)
            {
                errors.Add($"Verse {verseNumber} topic {code} has a probability outside 0 to 1.");
                continue;
            }

            verdicts.Add(new TopicVerdict(
                verseNumber,
                pair.Value.Topic,
                Decide(probability),
                probability));
        }

        if (errors.Count > 0)
            verdicts.Clear();

        return new TopicChecked(errors, verdicts);
    }

    public static string Decide(decimal probability)
    {
        if (probability >= YesAt)
            return LabelRunTopicAnswer.Yes;

        if (probability <= NoAt)
            return LabelRunTopicAnswer.No;

        return LabelRunTopicAnswer.Ambiguous;
    }
}

sealed class JevResponse
{
    public Dictionary<string, JevAnswer>? Answers { get; set; }
}

sealed class JevAnswer
{
    public string? Type { get; set; }
    public decimal? Noul { get; set; }
}

sealed record TopicChecked(IReadOnlyList<string> Errors, IReadOnlyList<TopicVerdict> Verdicts);

sealed record TopicVerdict(int VerseNumber, RunTopic Topic, string Answer, decimal Probability);
