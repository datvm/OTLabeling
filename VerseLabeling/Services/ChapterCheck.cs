namespace VerseLabeling.Services;

static class ChapterCheck
{
    internal static readonly Dictionary<string, string?> FixedParent = new(StringComparer.Ordinal)
    {
        ["People"] = null,
        ["Places"] = null,
        ["Israelite"] = "People",
        ["Non-Israelite"] = "People",
        ["Pre-Israelite"] = "People",
        ["Everyone"] = "People",
        ["Unknown"] = "People",
        ["Antediluvian"] = "People",
        ["Non-Israel"] = "Places",
    };

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

    public static IReadOnlyList<string> Check(
        ChapterOutput output,
        string bookCode,
        int chapter,
        IReadOnlyList<BibleVerse> verses)
    {
        var errors = new List<string>();
        var beforeIsrael = bookCode.Equals("GEN", StringComparison.OrdinalIgnoreCase) && chapter < 12;
        var catalog = TagCatalog.Create(output.Tags);
        errors.AddRange(catalog.Errors);
        foreach (var name in catalog.Names.Distinct(StringComparer.Ordinal))
        {
            if (beforeIsrael && (name == "Israelite" || name == "Non-Israelite"))
                errors.Add($"{name} appears before Genesis 12.");

            if (!beforeIsrael && name == "Pre-Israelite")
            {
                errors.Add(bookCode.Equals("GEN", StringComparison.OrdinalIgnoreCase)
                    ? "Pre-Israelite appears from Genesis 12 onward."
                    : "Pre-Israelite appears outside Genesis 1-11.");
            }
        }

        if (output.Verses is null)
        {
            errors.Add("Verses is absent.");
        }
        else
        {
            var expected = verses.Select(verse => verse.VerseNumber).ToHashSet();
            var seen = new HashSet<int>();
            foreach (var item in output.Verses)
            {
                if (!seen.Add(item.VerseNumber))
                    errors.Add($"Verse {item.VerseNumber} is repeated.");
                else if (!expected.Contains(item.VerseNumber))
                    errors.Add($"Verse {item.VerseNumber} is not in the chapter.");

                CheckVerseTags(item, catalog, beforeIsrael, bookCode, errors);
            }

            foreach (var number in expected.Order())
            {
                if (!seen.Contains(number))
                    errors.Add($"Verse {number} is absent.");
            }
        }

        if (!HasWords(output.SummarySoFar))
            errors.Add("SummarySoFar has no words.");

        if (!HasWords(output.ImmediateContext))
            errors.Add("ImmediateContext has no words.");

        return errors;
    }

    public static void DistinctTags(ChapterOutput output)
    {
        if (output.Tags is not null)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var distinct = new List<TagOutput>();
            foreach (var tag in output.Tags)
            {
                if (tag.Parents is not null)
                    tag.Parents = tag.Parents.Distinct(StringComparer.Ordinal).ToList();

                tag.Subclassifier = NormalizeSubclassifier(tag.Subclassifier);
                if (string.IsNullOrWhiteSpace(tag.Name))
                {
                    distinct.Add(tag);
                    continue;
                }

                var key = TagLabel(tag.Name, tag.Subclassifier) + "\n" + string.Join('\n', tag.Parents ?? []);
                if (seen.Add(key))
                    distinct.Add(tag);
            }

            output.Tags = distinct;
        }

        if (output.Verses is null)
            return;

        foreach (var verse in output.Verses)
        {
            if (verse.Tags is null)
                continue;

            verse.Tags = verse.Tags.Distinct(StringComparer.Ordinal).ToList();
        }
    }

    public static bool TryResolveTag(
        IReadOnlyList<TagOutput>? tags,
        string text,
        out TagOutput? match,
        out string? error) =>
        TagCatalog.Create(tags).TryResolve(text, null, out match, out error);

    public static string TagLabel(string name, string? subclassifier)
    {
        var text = NormalizeSubclassifier(subclassifier);
        return text.Length == 0 ? name : $"{name} ({text})";
    }

    public static string NormalizeSubclassifier(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    static void CheckVerseTags(
        VerseOutput item,
        TagCatalog catalog,
        bool beforeIsrael,
        string bookCode,
        List<string> errors)
    {
        if (item.Tags is null)
        {
            errors.Add($"Verse {item.VerseNumber} Tags is absent.");
            return;
        }

        foreach (var tag in item.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                errors.Add($"Verse {item.VerseNumber} has a blank tag.");
                continue;
            }

            RejectFixedCase(tag, $"Verse {item.VerseNumber} tag '{tag}'", errors);
            if (catalog.TryResolve(tag, item.Tags, out _, out var error))
                continue;

            errors.Add($"Verse {item.VerseNumber} tag '{tag}' {error}.");
            if (error != "is not in Tags")
                continue;

            if (beforeIsrael && (tag == "Israelite" || tag == "Non-Israelite"))
                errors.Add($"Verse {item.VerseNumber} has {tag} before Genesis 12.");

            if (!beforeIsrael && tag == "Pre-Israelite")
            {
                errors.Add(bookCode.Equals("GEN", StringComparison.OrdinalIgnoreCase)
                    ? $"Verse {item.VerseNumber} has Pre-Israelite from Genesis 12 onward."
                    : $"Verse {item.VerseNumber} has Pre-Israelite outside Genesis 1-11.");
            }
        }
    }


    internal static void RejectFixedCase(string tag, string label, List<string> errors)
    {
        var root = FixedParent.Keys.FirstOrDefault(name => string.Equals(name, tag, StringComparison.OrdinalIgnoreCase));
        if (root is not null && !string.Equals(root, tag, StringComparison.Ordinal))
            errors.Add($"{label} differs from {root} only by letter case.");
    }

    internal static bool IsRoot(string name) =>
        FixedParent.TryGetValue(name, out var parent) && parent is null;

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
            RejectWrongCase(property.Name, "Tags", "Verses", "SummarySoFar", "ImmediateContext");
            if (property.NameEquals("SummarySoFar") && property.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new LabelingException("A JSON value has the wrong type.");

            if (property.NameEquals("ImmediateContext") && property.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                throw new LabelingException("A JSON value has the wrong type.");

            if (property.NameEquals("Tags"))
                RejectTagCatalog(property.Value);
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

                if (!property.NameEquals("Tags"))
                    continue;

                if (property.Value.ValueKind is not JsonValueKind.Array and not JsonValueKind.Null)
                    throw new LabelingException("A JSON value has the wrong type.");

                if (property.Value.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var tag in property.Value.EnumerateArray())
                {
                    if (tag.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                        throw new LabelingException("A JSON value has the wrong type.");
                }
            }
        }
    }

    static void RejectTagCatalog(JsonElement tags)
    {
        if (tags.ValueKind is not JsonValueKind.Array and not JsonValueKind.Null)
            throw new LabelingException("A JSON value has the wrong type.");

        if (tags.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in tags.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new LabelingException("A JSON value has the wrong type.");

            foreach (var property in item.EnumerateObject())
            {
                RejectWrongCase(property.Name, "Name", "Parents", "Subclassifier");
                if (property.NameEquals("Name") && property.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                    throw new LabelingException("A JSON value has the wrong type.");

                if (property.NameEquals("Subclassifier") && property.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                    throw new LabelingException("A JSON value has the wrong type.");

                if (!property.NameEquals("Parents"))
                    continue;

                if (property.Value.ValueKind is not JsonValueKind.Array and not JsonValueKind.Null)
                    throw new LabelingException("A JSON value has the wrong type.");

                if (property.Value.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var parent in property.Value.EnumerateArray())
                {
                    if (parent.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
                        throw new LabelingException("A JSON value has the wrong type.");
                }
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
    public List<TagOutput>? Tags { get; set; }
    public List<VerseOutput>? Verses { get; set; }
    public string? SummarySoFar { get; set; }
    public string? ImmediateContext { get; set; }
}

sealed class TagOutput
{
    public string? Name { get; set; }
    public string? Subclassifier { get; set; }
    public List<string?>? Parents { get; set; }
}

sealed class VerseOutput
{
    public int VerseNumber { get; set; }
    public List<string?>? Tags { get; set; }
}
