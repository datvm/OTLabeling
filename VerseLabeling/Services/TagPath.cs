namespace VerseLabeling.Services;

sealed class TagPathSet
{
    public List<string> Errors { get; } = [];
    public List<TagOutput> Tags { get; } = [];
    public Dictionary<string, TagOutput> LeafByPath { get; } = new(StringComparer.Ordinal);
    public Dictionary<TagOutput, List<string>> ParentNames { get; } = [];
    public Dictionary<TagOutput, TagOutput> ParentNode { get; } = [];
    public HashSet<string> Names { get; } = new(StringComparer.Ordinal);

    public static TagPathSet From(IReadOnlyList<VerseOutput>? verses)
    {
        var set = new TagPathSet();
        if (verses is null)
            return set;

        var parsed = new List<(int Verse, string Text, Segment[] Segments)>();
        var failed = false;
        foreach (var item in verses)
        {
            if (item.Tags is null)
            {
                set.Errors.Add($"Verse {item.VerseNumber} Tags is absent.");
                failed = true;
                continue;
            }

            foreach (var tag in item.Tags)
            {
                var label = $"Verse {item.VerseNumber} tag '{tag}'";
                if (string.IsNullOrWhiteSpace(tag))
                {
                    set.Errors.Add($"Verse {item.VerseNumber} has a blank tag.");
                    failed = true;
                    continue;
                }

                if (!TryParse(tag, label, set.Errors, out var segments))
                {
                    failed = true;
                    continue;
                }

                foreach (var segment in segments)
                    set.Names.Add(segment.Name);

                parsed.Add((item.VerseNumber, tag, segments));
            }
        }

        set.RejectUnused(parsed);
        if (!failed && set.Errors.Count == 0)
            set.Build(parsed);

        return set;
    }

    static bool TryParse(string text, string label, List<string> errors, out Segment[] segments)
    {
        segments = [];
        var parts = text.Split('.');
        if (parts.Length == 1)
        {
            var only = parts[0].Trim();
            if (only == "God")
            {
                segments = [new Segment("God", "")];
                return true;
            }

            if (only is "People" or "Places")
            {
                errors.Add($"{label} has fixed children. Use a leaf fixed tag.");
                return false;
            }

            errors.Add($"{label} is not a path.");
            return false;
        }

        var parsed = new List<Segment>(parts.Length);
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index].Trim();
            if (part.Length == 0)
            {
                errors.Add($"{label} is not a path.");
                return false;
            }

            var last = index == parts.Length - 1;
            if (!last && (part.Contains('(') || part.Contains(')')))
            {
                errors.Add($"{label} is not a path.");
                return false;
            }

            if (!TrySegment(part, last, label, errors, out var segment))
                return false;

            parsed.Add(segment);
        }

        if (!ValidShape(parsed, label, errors))
            return false;

        segments = parsed.ToArray();
        return true;
    }

    static bool TrySegment(string part, bool last, string label, List<string> errors, out Segment segment)
    {
        segment = default;
        var name = part;
        var subclassifier = "";
        if (last)
        {
            var open = part.LastIndexOf(" (", StringComparison.Ordinal);
            if (open > 0 && part.EndsWith(')') && part.IndexOf('(', StringComparison.Ordinal) == open + 1)
            {
                name = part[..open];
                subclassifier = part[(open + 2)..^1].Trim();
                if (subclassifier.Length == 0)
                {
                    errors.Add($"{label} is not a path.");
                    return false;
                }
            }
            else if (part.Contains('(') || part.Contains(')'))
            {
                errors.Add($"{label} is not a path.");
                return false;
            }
        }

        if (name.Length == 0 || subclassifier.Contains('(') || subclassifier.Contains(')'))
        {
            errors.Add($"{label} is not a path.");
            return false;
        }

        var before = errors.Count;
        ChapterCheck.RejectFixedCase(name, label, errors);
        if (errors.Count != before)
            return false;

        if (ChapterCheck.ClassifierOnLockedName(name, out var locked))
        {
            errors.Add($"{label} puts a classifier on {locked}.");
            return false;
        }

        if (subclassifier.Length > 0 && ChapterCheck.IsLockedName(name))
        {
            errors.Add($"Tag '{name}' does not take a Subclassifier.");
            return false;
        }

        if (subclassifier.Length > ChapterCheck.MaxSubclassifierLength)
        {
            errors.Add($"Tag '{name}' Subclassifier is too long.");
            return false;
        }

        if (name is "Everyone")
        {
            errors.Add("Tag 'Everyone' is not used.");
            return false;
        }

        if (name is "Unknown")
        {
            errors.Add("Tag 'Unknown' is not used. Use Others.");
            return false;
        }

        segment = new Segment(name, subclassifier);
        return true;
    }

    static bool ValidShape(List<Segment> segments, string label, List<string> errors)
    {
        var root = segments[0].Name;
        if (root is not ("People" or "God" or "Places"))
        {
            errors.Add($"{label} does not start with People, God, or Places.");
            return false;
        }

        if (segments[0].Subclassifier.Length > 0)
        {
            errors.Add($"Tag '{root}' does not take a Subclassifier.");
            return false;
        }

        for (var index = 1; index < segments.Count; index++)
        {
            if (!segments[index].Name.Equals(segments[index - 1].Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var stop = string.Join('.', segments.Take(index).Select(segment => segment.Name));
            errors.Add($"{label} repeats {segments[index].Name}. Use {stop}.");
            return false;
        }

        if (root == "God")
        {
            for (var index = 1; index < segments.Count; index++)
            {
                if (ChapterCheck.IsLockedName(segments[index].Name))
                {
                    errors.Add(WrongPlace(label, segments[index].Name, NextName(segments, index)));
                    return false;
                }
            }

            return true;
        }

        if (segments.Count == 1)
        {
            errors.Add($"{label} is not a path.");
            return false;
        }

        var bucket = segments[1].Name;
        var allowed = root == "People"
            ? bucket is "Antediluvian" or "Pre-Israelite" or "Israelite" or "Non-Israelite" or "Others"
            : bucket is "Israel" or "Non-Israel" or "Others";
        if (!allowed)
        {
            errors.Add($"{label} cannot have the parent {root}.");
            return false;
        }

        if (bucket == "Israel" && segments.Count > 2 && segments.Skip(2).Any(segment => segment.Name == "Israel"))
        {
            errors.Add(WrongPlace(label, "Israel", null));
            return false;
        }

        for (var index = 2; index < segments.Count; index++)
        {
            var name = segments[index].Name;
            var personIsrael = root == "People" && bucket == "Israelite" && name == "Israel" && index == 2;
            if (personIsrael)
            {
                if (segments.Count > 3)
                {
                    errors.Add(WrongPlace(label, "Israel", segments[3].Name));
                    return false;
                }

                continue;
            }

            if (ChapterCheck.IsLockedName(name) || ChapterCheck.IsRoot(name))
            {
                errors.Add(WrongPlace(label, name, NextName(segments, index)));
                return false;
            }
        }

        return FreeSteps(segments, 2, label, errors);
    }

    static string? NextName(List<Segment> segments, int index) =>
        index + 1 < segments.Count ? segments[index + 1].Name : null;

    static string WrongPlace(string label, string name, string? next)
    {
        if (name == "Israel")
        {
            var stop = "Israel the person is People.Israelite.Israel and that path stops there. Israel the land is Places.Israel.";
            if (next is null)
                return $"{label} uses Israel in the wrong place. {stop}";

            return $"{label} uses Israel in the wrong place. {stop} Put {next} under People.Israelite, as People.Israelite.{next}.";
        }

        if (name == "God")
            return $"{label} uses God in the wrong place. A name of God is one step under God, such as God.LORD.";

        if (name is "People" or "Places")
            return $"{label} uses {name} in the wrong place. {name} starts its own path.";

        if (ChapterCheck.FixedParent.TryGetValue(name, out var parent) && parent is not null)
            return $"{label} uses {name} in the wrong place. Use {parent}.{name}.";

        if (name == "Others")
            return $"{label} uses Others in the wrong place. Others is a child of People or Places. A further name continues under it, such as People.Others.Job.";

        return $"{label} uses {name} in the wrong place.";
    }

    static bool FreeSteps(List<Segment> segments, int start, string label, List<string> errors)
    {
        for (var index = start; index < segments.Count; index++)
        {
            if (segments[index].Name is "People" or "God" or "Places")
            {
                errors.Add(WrongPlace(label, segments[index].Name, null));
                return false;
            }
        }

        return true;
    }

    void RejectUnused(List<(int Verse, string Text, Segment[] Segments)> parsed)
    {
        foreach (var group in parsed.GroupBy(
            item => string.Join('.', item.Segments.Select(segment => segment.Name)),
            StringComparer.OrdinalIgnoreCase))
        {
            var phrases = group.Select(item => item.Segments[^1].Subclassifier).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!phrases.Any(phrase => phrase.Length == 0) || !phrases.Any(phrase => phrase.Length > 0))
                continue;

            var name = group.First().Segments[^1].Name;
            var bare = string.Join(
                ", ",
                group
                    .Where(item => item.Segments[^1].Subclassifier.Length == 0)
                    .Select(item => item.Text)
                    .Distinct(StringComparer.OrdinalIgnoreCase));
            foreach (var text in group
                .Where(item => item.Segments[^1].Subclassifier.Length > 0)
                .Select(item => item.Text)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Errors.Add($"Tag '{text}' sits beside {bare}. Two different people or places named {name} each need a Subclassifier. Add one to {bare} as well. A wife, son, family, or other companion is not a second {name}. Give that companion its own tag name, such as {name}'s wife.");
            }
        }
    }

    void Build(List<(int Verse, string Text, Segment[] Segments)> parsed)
    {
        var nodes = new Dictionary<string, TagOutput>(StringComparer.OrdinalIgnoreCase);
        var parentNames = new Dictionary<TagOutput, List<string>>();
        foreach (var (_, text, segments) in parsed)
        {
            string? parentLabel = null;
            TagOutput? parentNode = null;
            var key = "";
            var names = new List<string>();
            TagOutput? leaf = null;
            foreach (var segment in segments)
            {
                var label = ChapterCheck.TagLabel(segment.Name, segment.Subclassifier);
                key = key.Length == 0 ? label : key + "." + label;
                if (!nodes.TryGetValue(key, out var node))
                {
                    node = new TagOutput
                    {
                        Name = segment.Name,
                        Subclassifier = segment.Subclassifier,
                        Parents = parentLabel is null ? [] : [parentLabel],
                    };
                    nodes.Add(key, node);
                    parentNames.Add(node, names.ToList());
                    if (parentNode is not null)
                        ParentNode[node] = parentNode;
                }

                names.Add(segment.Name);
                parentLabel = label;
                parentNode = node;
                leaf = node;
            }

            LeafByPath[text] = leaf!;
        }

        foreach (var pair in parentNames)
            ParentNames[pair.Key] = pair.Value;

        Tags.AddRange(nodes.Values);
        RejectSharedLeaves(errors: Errors);
    }

    void RejectSharedLeaves(List<string> errors)
    {
        foreach (var group in Tags.GroupBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase))
        {
            var bare = group.Where(tag => string.IsNullOrEmpty(tag.Subclassifier)).ToList();
            for (var i = 0; i < bare.Count; i++)
            {
                for (var j = i + 1; j < bare.Count; j++)
                {
                    if (RootOf(bare[i]).Equals(RootOf(bare[j]), StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Tag '{group.Key}' needs a Subclassifier.");
                        i = bare.Count;
                        break;
                    }
                }
            }
        }
    }

    string RootOf(TagOutput tag)
    {
        var names = ParentNames[tag];
        return names.Count == 0 ? tag.Name! : names[0];
    }

    readonly record struct Segment(string Name, string Subclassifier);
}
