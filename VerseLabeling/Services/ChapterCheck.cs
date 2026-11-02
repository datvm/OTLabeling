namespace VerseLabeling.Services;

static class ChapterCheck
{
    static readonly Dictionary<string, string?> FixedParent = new(StringComparer.Ordinal)
    {
        ["People"] = null,
        ["Places"] = null,
        ["Israelite"] = "People",
        ["Non-Israelite"] = "People",
        ["Pre-Israelite"] = "People",
        ["Everyone"] = "People",
        ["Unknown"] = "People",
        ["Antediluvian"] = "People",
        ["Israel"] = "Places",
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
        bool beforeJoshua,
        IReadOnlyList<BibleVerse> verses)
    {
        var errors = new List<string>();
        var beforeIsrael = bookCode.Equals("GEN", StringComparison.OrdinalIgnoreCase) && chapter < 12;
        var catalog = ReadCatalog(output.Tags, beforeIsrael, beforeJoshua, bookCode, errors);

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

                CheckVerseTags(item, catalog, beforeIsrael, beforeJoshua, bookCode, errors);
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
            foreach (var tag in output.Tags)
            {
                if (tag.Parents is null)
                    continue;

                tag.Parents = tag.Parents.Distinct(StringComparer.Ordinal).ToList();
            }
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

    static Dictionary<string, List<string>>? ReadCatalog(
        List<TagOutput>? tags,
        bool beforeIsrael,
        bool beforeJoshua,
        string bookCode,
        List<string> errors)
    {
        if (tags is null)
        {
            errors.Add("Tags is absent.");
            return null;
        }

        var byName = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var parentsAbsent = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Name))
            {
                errors.Add("A tag name is blank.");
                continue;
            }

            RejectFixedCase(tag.Name, $"Tag '{tag.Name}'", errors);
            if (!byName.ContainsKey(tag.Name))
                byName.Add(tag.Name, []);
            else
                errors.Add($"Tag '{tag.Name}' is repeated.");

            if (tag.Parents is null)
            {
                errors.Add($"Tag '{tag.Name}' Parents is absent.");
                parentsAbsent.Add(tag.Name);
                continue;
            }

            var parents = new List<string>();
            foreach (var parent in tag.Parents)
            {
                if (string.IsNullOrWhiteSpace(parent))
                {
                    errors.Add($"Tag '{tag.Name}' has a blank parent.");
                    continue;
                }

                RejectFixedCase(parent, $"Tag '{tag.Name}' parent '{parent}'", errors);
                parents.Add(parent);
            }

            byName[tag.Name] = parents;
        }

        foreach (var name in byName.Keys)
        {
            if (beforeIsrael && (name == "Israelite" || name == "Non-Israelite"))
                errors.Add($"{name} appears before Genesis 12.");

            if (!beforeIsrael && name == "Pre-Israelite")
            {
                errors.Add(bookCode.Equals("GEN", StringComparison.OrdinalIgnoreCase)
                    ? "Pre-Israelite appears from Genesis 12 onward."
                    : "Pre-Israelite appears outside Genesis 1-11.");
            }

            if (beforeJoshua && name == "Israel")
                errors.Add("Israel appears before Joshua.");
        }

        foreach (var (name, parents) in byName)
        {
            if (parentsAbsent.Contains(name))
                continue;

            if (FixedParent.TryGetValue(name, out var required))
            {
                if (required is null && parents.Count > 0)
                    errors.Add($"Root tag {name} has a parent.");

                if (required is not null && (parents.Count != 1 || parents[0] != required))
                    errors.Add($"Tag '{name}' must have the parent {required}.");
            }
            else if (parents.Count == 0)
            {
                errors.Add($"Tag '{name}' has no parent.");
            }

            foreach (var parent in parents)
            {
                if (parent == name)
                    errors.Add($"Tag '{name}' is its own parent.");
                else if (!byName.ContainsKey(parent))
                    errors.Add($"Tag '{name}' parent '{parent}' is not in Tags.");
            }
        }

        var color = new Dictionary<string, int>(StringComparer.Ordinal);
        var cyclic = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in byName.Keys)
            Walk(name, byName, color, cyclic, errors);

        foreach (var (name, parents) in byName)
        {
            if (IsRoot(name) || parents.Count == 0 || cyclic.Contains(name))
                continue;

            if (parents.Any(parent => parent == name || !byName.ContainsKey(parent)))
                continue;

            if (!ReachesRoot(name, byName, []))
                errors.Add($"Tag '{name}' has no path to a root tag.");
        }

        return byName;
    }

    static void CheckVerseTags(
        VerseOutput item,
        Dictionary<string, List<string>>? catalog,
        bool beforeIsrael,
        bool beforeJoshua,
        string bookCode,
        List<string> errors)
    {
        if (item.Tags is null)
        {
            errors.Add($"Verse {item.VerseNumber} Tags is absent.");
            return;
        }

        var names = new List<string>();
        foreach (var tag in item.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                errors.Add($"Verse {item.VerseNumber} has a blank tag.");
                continue;
            }

            RejectFixedCase(tag, $"Verse {item.VerseNumber} tag '{tag}'", errors);
            if (catalog is null || !catalog.ContainsKey(tag))
            {
                if (beforeIsrael && (tag == "Israelite" || tag == "Non-Israelite"))
                    errors.Add($"Verse {item.VerseNumber} has {tag} before Genesis 12.");

                if (!beforeIsrael && tag == "Pre-Israelite")
                {
                    errors.Add(bookCode.Equals("GEN", StringComparison.OrdinalIgnoreCase)
                        ? $"Verse {item.VerseNumber} has Pre-Israelite from Genesis 12 onward."
                        : $"Verse {item.VerseNumber} has Pre-Israelite outside Genesis 1-11.");
                }

                if (beforeJoshua && tag == "Israel")
                    errors.Add($"Verse {item.VerseNumber} has Israel before Joshua.");
            }

            names.Add(tag);
            if (catalog is not null && !catalog.ContainsKey(tag))
                errors.Add($"Verse {item.VerseNumber} tag '{tag}' is not in Tags.");
        }

        if (catalog is null)
            return;

        foreach (var name in names)
        {
            if (!catalog.ContainsKey(name))
                continue;

            var ancestors = new HashSet<string>(StringComparer.Ordinal);
            CollectAncestors(name, catalog, ancestors, []);
            foreach (var other in names)
            {
                if (ancestors.Contains(other))
                    errors.Add($"Verse {item.VerseNumber} includes '{name}' and its ancestor '{other}'.");
            }
        }
    }

    static void Walk(
        string name,
        Dictionary<string, List<string>> byName,
        Dictionary<string, int> color,
        HashSet<string> cyclic,
        List<string> errors)
    {
        if (cyclic.Contains(name))
            return;

        if (color.TryGetValue(name, out var state))
        {
            if (state == 1)
            {
                errors.Add($"Tag '{name}' is its own ancestor.");
                cyclic.Add(name);
                color[name] = 2;
            }

            return;
        }

        color[name] = 1;
        foreach (var parent in byName[name])
        {
            if (parent == name || !byName.ContainsKey(parent))
                continue;

            Walk(parent, byName, color, cyclic, errors);
            if (cyclic.Contains(parent))
                cyclic.Add(name);
        }

        color[name] = 2;
    }

    static bool ReachesRoot(string name, Dictionary<string, List<string>> byName, HashSet<string> stack)
    {
        if (!stack.Add(name))
            return false;

        var parents = byName[name];
        if (parents.Count == 0)
            return IsRoot(name);

        foreach (var parent in parents)
        {
            if (byName.ContainsKey(parent) && ReachesRoot(parent, byName, stack))
                return true;
        }

        return false;
    }

    static void CollectAncestors(
        string name,
        Dictionary<string, List<string>> catalog,
        HashSet<string> ancestors,
        HashSet<string> stack)
    {
        if (!stack.Add(name) || !catalog.TryGetValue(name, out var parents))
            return;

        foreach (var parent in parents)
        {
            if (!ancestors.Add(parent))
                continue;

            CollectAncestors(parent, catalog, ancestors, stack);
        }
    }

    static void RejectFixedCase(string tag, string label, List<string> errors)
    {
        var root = FixedParent.Keys.FirstOrDefault(name => string.Equals(name, tag, StringComparison.OrdinalIgnoreCase));
        if (root is not null && !string.Equals(root, tag, StringComparison.Ordinal))
            errors.Add($"{label} differs from {root} only by letter case.");
    }

    static bool IsRoot(string name) =>
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
                RejectWrongCase(property.Name, "Name", "Parents");
                if (property.NameEquals("Name") && property.Value.ValueKind is not JsonValueKind.String and not JsonValueKind.Null)
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
    public List<string?>? Parents { get; set; }
}

sealed class VerseOutput
{
    public int VerseNumber { get; set; }
    public List<string?>? Tags { get; set; }
}
