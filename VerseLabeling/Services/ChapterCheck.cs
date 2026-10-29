namespace VerseLabeling.Services;

static class ChapterCheck
{
    static readonly string[] FixedNames = ["Israelite", "Non-Israelite", "Everyone", "Unknown"];

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
    };

    public static ChapterOutput Parse(string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body.Trim());
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
        });

        try
        {
            RejectWrongPropertyCase(body);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new LabelingException("The response is not one JSON object.");

            var output = JsonSerializer.Deserialize<ChapterOutput>(ref reader, JsonOptions)
                ?? throw new LabelingException("The response is not one JSON object.");

            if (reader.Read())
                throw new LabelingException("The response is not one JSON object.");

            return output;
        }
        catch (LabelingException)
        {
            throw;
        }
        catch (JsonException ex) when (ex.Message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase))
        {
            throw new LabelingException("A JSON value has the wrong type.");
        }
        catch (JsonException)
        {
            throw new LabelingException("The response is not one JSON object.");
        }
    }

    public static IReadOnlyList<string> Check(ChapterOutput output, IReadOnlyList<BibleVerse> verses)
    {
        var errors = new List<string>();
        if (output.Verses is null)
        {
            errors.Add("Verses is absent.");
            return errors;
        }

        var expected = verses.Select(verse => verse.VerseNumber).ToHashSet();
        var seen = new HashSet<int>();
        foreach (var item in output.Verses)
        {
            if (!seen.Add(item.VerseNumber))
                errors.Add($"Verse {item.VerseNumber} is repeated.");
            else if (!expected.Contains(item.VerseNumber))
                errors.Add($"Verse {item.VerseNumber} is not in the chapter.");

            CheckTags(item, errors);
        }

        foreach (var number in expected.Order())
        {
            if (!seen.Contains(number))
                errors.Add($"Verse {number} is absent.");
        }

        if (!HasWords(output.SummarySoFar))
            errors.Add("SummarySoFar has no words.");

        if (!HasWords(output.ImmediateContext))
            errors.Add("ImmediateContext has no words.");

        return errors;
    }

    static void CheckTags(VerseOutput item, List<string> errors)
    {
        if (item.Tags is null)
        {
            errors.Add($"Verse {item.VerseNumber} Tags is absent.");
            return;
        }

        if (item.Tags.Count == 0)
        {
            errors.Add($"Verse {item.VerseNumber} Tags is empty.");
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var hasEveryone = false;
        foreach (var tag in item.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                errors.Add($"Verse {item.VerseNumber} has a blank tag.");
                continue;
            }

            if (!seen.Add(tag))
                errors.Add($"Verse {item.VerseNumber} repeats tag '{tag}'.");

            var fixedName = FixedNames.FirstOrDefault(name =>
                string.Equals(name, tag, StringComparison.OrdinalIgnoreCase));
            if (fixedName is not null && !string.Equals(fixedName, tag, StringComparison.Ordinal))
                errors.Add($"Verse {item.VerseNumber} tag '{tag}' differs from {fixedName} only by letter case.");

            if (tag == "Everyone")
                hasEveryone = true;
        }

        if (hasEveryone && item.Tags.Count != 1)
            errors.Add($"Verse {item.VerseNumber} has Everyone with another tag.");
    }

    static bool HasWords(string? text) =>
        text?.Any(char.IsLetterOrDigit) == true;

    static void RejectWrongPropertyCase(string body)
    {
        using var document = JsonDocument.Parse(body, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
        });

        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new LabelingException("The response is not one JSON object.");

        foreach (var property in document.RootElement.EnumerateObject())
        {
            RejectWrongCase(property.Name, "Verses", "SummarySoFar", "ImmediateContext");
            if (property.NameEquals("SummarySoFar") && property.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new LabelingException("A JSON value has the wrong type.");

            if (property.NameEquals("ImmediateContext") && property.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new LabelingException("A JSON value has the wrong type.");
        }

        if (!document.RootElement.TryGetProperty("Verses", out var verses))
            return;

        if (verses.ValueKind != JsonValueKind.Array)
            throw new LabelingException("A JSON value has the wrong type.");

        foreach (var item in verses.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new LabelingException("A JSON value has the wrong type.");

            foreach (var property in item.EnumerateObject())
            {
                RejectWrongCase(property.Name, "VerseNumber", "Tags");
                if (property.NameEquals("VerseNumber") && property.Value.ValueKind is not JsonValueKind.Number and not JsonValueKind.Null)
                    throw new LabelingException("A JSON value has the wrong type.");

                if (property.NameEquals("Tags") && property.Value.ValueKind is not JsonValueKind.Array and not JsonValueKind.Null)
                    throw new LabelingException("A JSON value has the wrong type.");
            }
        }
    }

    static void RejectWrongCase(string name, params string[] knownNames)
    {
        foreach (var known in knownNames)
        {
            if (name.Equals(known, StringComparison.OrdinalIgnoreCase) &&
                !name.Equals(known, StringComparison.Ordinal))
            {
                throw new LabelingException($"Property name '{name}' is not PascalCase.");
            }
        }
    }
}

sealed class ChapterOutput
{
    public List<VerseOutput>? Verses { get; set; }
    public string? SummarySoFar { get; set; }
    public string? ImmediateContext { get; set; }
}

sealed class VerseOutput
{
    public int VerseNumber { get; set; }
    public List<string?>? Tags { get; set; }
}
