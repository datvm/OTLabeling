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

        var parsed = new List<(string Text, Segment[] Segments)>();
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

                parsed.Add((tag, segments));
            }
        }

        if (!failed)
            set.Build(parsed);

        return set;
    }

    static bool TryParse(string text, string label, List<string> errors, out Segment[] segments)
    {
        segments = [];
        var parts = text.Split('.');
        if (parts.Length == 1)
        {
            if (parts[0].Trim() == "God")
            {
                segments = [new Segment("God", "")];
                return true;
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
            var close = part.EndsWith(')') ? part.Length - 1 : -1;
            if (open > 0 && close == part.Length - 1 && part.IndexOf('(', StringComparison.Ordinal) == open)
            {
                name = part[..open];
                subclassifier = part[(open + 2)..close].Trim();
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

        if (root == "God")
        {
            for (var index = 1; index < segments.Count; index++)
            {
                if (ChapterCheck.IsLockedName(segments[index].Name))
                {
                    errors.Add($"{label} uses {segments[index].Name} in the wrong place.");
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

        if (bucket == "Others" && segments.Count > 2)
        {
            errors.Add($"{label} adds a child under Others.");
            return false;
        }

        if (bucket == "Israel" && segments.Count > 2 && segments.Skip(2).Any(segment => segment.Name == "Israel"))
        {
            errors.Add($"{label} uses Israel in the wrong place.");
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
                    errors.Add($"{label} uses Israel in the wrong place.");
                    return false;
                }

                continue;
            }

            if (ChapterCheck.IsLockedName(name) || ChapterCheck.IsRoot(name))
            {
                errors.Add($"{label} uses {name} in the wrong place.");
                return false;
            }
        }

        return FreeSteps(segments, 2, label, errors);
    }

    static bool FreeSteps(List<Segment> segments, int start, string label, List<string> errors)
    {
        for (var index = start; index < segments.Count; index++)
        {
            if (segments[index].Name is "People" or "God" or "Places")
            {
                errors.Add($"{label} uses {segments[index].Name} in the wrong place.");
                return false;
            }
        }

        return true;
    }

    void Build(List<(string Text, Segment[] Segments)> parsed)
    {
        var nodes = new Dictionary<string, TagOutput>(StringComparer.Ordinal);
        var parentNames = new Dictionary<TagOutput, List<string>>();
        foreach (var (text, segments) in parsed)
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
        foreach (var group in Tags.GroupBy(tag => tag.Name, StringComparer.Ordinal))
        {
            var bare = group.Where(tag => string.IsNullOrEmpty(tag.Subclassifier)).ToList();
            for (var i = 0; i < bare.Count; i++)
            {
                for (var j = i + 1; j < bare.Count; j++)
                {
                    if (RootOf(bare[i]) == RootOf(bare[j]))
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
