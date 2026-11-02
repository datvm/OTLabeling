namespace VerseLabeling.Services;

sealed class TagCatalog
{
    readonly List<Entry> entries = [];
    readonly Dictionary<int, Entry> byId = [];
    readonly Dictionary<TagOutput, Entry> bySource = [];
    readonly Dictionary<int, List<int>> links = [];
    readonly Dictionary<int, HashSet<string>> roots = [];
    readonly Dictionary<int, HashSet<int>> ancestors = [];

    TagCatalog()
    {
    }

    public IReadOnlyList<string> Errors { get; private set; } = [];
    public IReadOnlyList<string> Names => entries.Select(entry => entry.Name).ToList();
    public IReadOnlyList<TagOutput> Tags => entries.Select(entry => entry.Source).ToList();

    public static TagCatalog Create(IReadOnlyList<TagOutput>? tags)
    {
        var catalog = new TagCatalog();
        var errors = new List<string>();
        if (tags is null)
        {
            errors.Add("Tags is absent.");
            catalog.Errors = errors;
            return catalog;
        }

        var nextId = 0;
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Name))
            {
                errors.Add("A tag name is blank.");
                continue;
            }

            ChapterCheck.RejectFixedCase(tag.Name, $"Tag '{tag.Name}'", errors);
            var subclassifier = ChapterCheck.NormalizeSubclassifier(tag.Subclassifier);
            var label = ChapterCheck.TagLabel(tag.Name, subclassifier);
            if (tag.Parents is null)
            {
                errors.Add($"Tag '{label}' Parents is absent.");
                catalog.Add(nextId++, tag, tag.Name, subclassifier, label, [], true);
                continue;
            }

            var parents = new List<string>();
            foreach (var parent in tag.Parents)
            {
                if (string.IsNullOrWhiteSpace(parent))
                {
                    errors.Add($"Tag '{label}' has a blank parent.");
                    continue;
                }

                ChapterCheck.RejectFixedCase(parent, $"Tag '{label}' parent '{parent}'", errors);
                parents.Add(parent);
            }

            catalog.Add(nextId++, tag, tag.Name, subclassifier, label, parents, false);
        }

        catalog.LinkParents(errors);
        catalog.FindCycles(errors);
        catalog.CollectRoots();
        catalog.RejectSharedNames(errors);
        catalog.Errors = errors;
        return catalog;
    }

    public bool TryResolve(
        string text,
        IReadOnlyList<string?>? context,
        out TagOutput? match,
        out string? error)
    {
        match = null;
        error = null;
        var hits = Matches(text);
        if (hits.Count == 0)
        {
            error = "is not in Tags";
            return false;
        }

        if (hits.Count == 1)
        {
            match = hits[0].Source;
            return true;
        }

        var contextIds = new HashSet<int>();
        foreach (var item in context ?? [])
        {
            if (string.IsNullOrWhiteSpace(item) || item == text)
                continue;

            var others = Matches(item);
            if (others.Count == 1)
                contextIds.Add(others[0].Id);
        }

        var selected = hits.Where(hit => ancestors[hit.Id].Overlaps(contextIds)).ToList();
        if (selected.Count == 1)
        {
            match = selected[0].Source;
            return true;
        }

        error = "matches more than one tag";
        return false;
    }

    public string StoredSubclassifier(TagOutput tag)
    {
        var subclassifier = ChapterCheck.NormalizeSubclassifier(tag.Subclassifier);
        if (subclassifier.Length > 0 || !bySource.TryGetValue(tag, out var entry))
            return subclassifier;

        var mine = roots[entry.Id];
        if (mine.Count != 1)
            return "";

        var sharesName = entries.Any(other =>
            other.Id != entry.Id &&
            other.Name == entry.Name &&
            other.Subclassifier.Length == 0 &&
            roots[other.Id].Count > 0 &&
            !roots[other.Id].Overlaps(mine));
        return sharesName ? mine.Single() : "";
    }

    void Add(
        int id,
        TagOutput source,
        string name,
        string subclassifier,
        string label,
        List<string> parents,
        bool parentsAbsent)
    {
        var entry = new Entry(id, source, name, subclassifier, label, parents, parentsAbsent);
        entries.Add(entry);
        byId[id] = entry;
        bySource[source] = entry;
        links[id] = [];
    }

    void LinkParents(List<string> errors)
    {
        var pending = new List<(Entry Entry, string Parent)>();
        foreach (var entry in entries)
        {
            if (entry.ParentsAbsent)
                continue;

            if (ChapterCheck.FixedParent.TryGetValue(entry.Name, out var required))
            {
                if (required is null && entry.Parents.Count > 0)
                    errors.Add($"Root tag {entry.Name} has a parent.");

                if (required is not null && (entry.Parents.Count != 1 || entry.Parents[0] != required))
                    errors.Add($"Tag '{entry.Name}' must have the parent {required}.");
            }
            else if (entry.Parents.Count == 0)
            {
                errors.Add($"Tag '{entry.Label}' has no parent.");
            }

            foreach (var parent in entry.Parents)
            {
                if (parent == entry.Label || (entry.Subclassifier.Length == 0 && parent == entry.Name))
                {
                    errors.Add($"Tag '{entry.Label}' is its own parent.");
                    continue;
                }

                var hits = Matches(parent);
                if (hits.Count == 0)
                {
                    errors.Add($"Tag '{entry.Label}' parent '{parent}' is not in Tags.");
                    continue;
                }

                if (hits.Count == 1)
                {
                    AddLink(entry.Id, hits[0].Id);
                    continue;
                }

                pending.Add((entry, parent));
            }
        }

        var progressed = true;
        while (pending.Count > 0 && progressed)
        {
            progressed = false;
            var next = new List<(Entry Entry, string Parent)>();
            foreach (var item in pending)
            {
                var contextRoots = new HashSet<string>(StringComparer.Ordinal);
                foreach (var parentId in links[item.Entry.Id])
                    contextRoots.UnionWith(RootNames(parentId, []));

                var chosen = Matches(item.Parent)
                    .Where(hit => RootNames(hit.Id, []).Overlaps(contextRoots))
                    .ToList();
                if (contextRoots.Count == 0 || chosen.Count != 1)
                {
                    next.Add(item);
                    continue;
                }

                AddLink(item.Entry.Id, chosen[0].Id);
                progressed = true;
            }

            pending = next;
        }

        foreach (var item in pending)
            errors.Add($"Tag '{item.Entry.Label}' parent '{item.Parent}' matches more than one tag.");
    }

    void FindCycles(List<string> errors)
    {
        var color = new Dictionary<int, int>();
        var cyclic = new HashSet<int>();
        foreach (var entry in entries)
            Walk(entry.Id, color, cyclic, errors);

        foreach (var entry in entries)
        {
            if (cyclic.Contains(entry.Id) || ChapterCheck.IsRoot(entry.Name) || links[entry.Id].Count == 0)
                continue;

            if (!ReachesRoot(entry.Id, []))
                errors.Add($"Tag '{entry.Label}' has no path to a root tag.");
        }
    }

    void CollectRoots()
    {
        foreach (var entry in entries)
        {
            roots[entry.Id] = RootNames(entry.Id, []);
            ancestors[entry.Id] = AncestorIds(entry.Id, []);
        }
    }

    void AddLink(int childId, int parentId)
    {
        if (!links[childId].Contains(parentId))
            links[childId].Add(parentId);
    }

    void RejectSharedNames(List<string> errors)
    {
        var noted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in entries.GroupBy(entry => entry.Label, StringComparer.Ordinal))
        {
            if (group.Count() < 2)
                continue;

            if (SplitByRoot(group.ToList()))
                continue;

            if (group.Any(entry => entry.Subclassifier.Length == 0))
            {
                errors.Add($"Tag '{group.Key}' needs a Subclassifier.");
                noted.Add(group.Key);
            }
            else
            {
                errors.Add($"Tag '{group.Key}' is repeated.");
            }
        }

        foreach (var group in entries.GroupBy(entry => entry.Name, StringComparer.Ordinal))
        {
            if (noted.Contains(group.Key))
                continue;

            var sharesRoot = group.Any(entry =>
                entry.Subclassifier.Length == 0 &&
                roots[entry.Id].Count > 0 &&
                group.Any(other => other.Id != entry.Id && roots[other.Id].Overlaps(roots[entry.Id])));
            if (sharesRoot)
                errors.Add($"Tag '{group.Key}' needs a Subclassifier.");
        }
    }

    bool SplitByRoot(IReadOnlyList<Entry> group)
    {
        if (group.Any(entry => entry.Subclassifier.Length > 0))
            return false;

        var sets = group.Select(entry => roots[entry.Id]).ToList();
        if (sets.Any(set => set.Count == 0))
            return false;

        for (var i = 0; i < sets.Count; i++)
        {
            for (var j = i + 1; j < sets.Count; j++)
            {
                if (sets[i].Overlaps(sets[j]))
                    return false;
            }
        }

        return true;
    }

    List<Entry> Matches(string text)
    {
        var byLabel = entries.Where(entry => entry.Label == text).ToList();
        if (byLabel.Count > 0)
            return byLabel;

        return entries.Where(entry => entry.Name == text).ToList();
    }

    void Walk(int id, Dictionary<int, int> color, HashSet<int> cyclic, List<string> errors)
    {
        if (cyclic.Contains(id))
            return;

        if (color.TryGetValue(id, out var state))
        {
            if (state == 1)
            {
                errors.Add($"Tag '{byId[id].Label}' is its own ancestor.");
                cyclic.Add(id);
                color[id] = 2;
            }

            return;
        }

        color[id] = 1;
        foreach (var parent in links[id])
        {
            if (parent == id)
                continue;

            Walk(parent, color, cyclic, errors);
            if (cyclic.Contains(parent))
                cyclic.Add(id);
        }

        color[id] = 2;
    }

    bool ReachesRoot(int id, HashSet<int> stack)
    {
        if (!stack.Add(id))
            return false;

        try
        {
            var parents = links[id];
            if (parents.Count == 0)
                return ChapterCheck.IsRoot(byId[id].Name);

            foreach (var parent in parents)
            {
                if (ReachesRoot(parent, stack))
                    return true;
            }

            return false;
        }
        finally
        {
            stack.Remove(id);
        }
    }

    HashSet<string> RootNames(int id, HashSet<int> stack)
    {
        if (!stack.Add(id))
            return [];

        var names = new HashSet<string>(StringComparer.Ordinal);
        var parents = links[id];
        if (parents.Count == 0)
        {
            var name = byId[id].Name;
            if (ChapterCheck.IsRoot(name))
                names.Add(name);
        }
        else
        {
            foreach (var parent in parents)
                names.UnionWith(RootNames(parent, stack));
        }

        stack.Remove(id);
        return names;
    }

    HashSet<int> AncestorIds(int id, HashSet<int> stack)
    {
        if (!stack.Add(id))
            return [];

        var ids = new HashSet<int>();
        foreach (var parent in links[id])
        {
            ids.Add(parent);
            ids.UnionWith(AncestorIds(parent, stack));
        }

        stack.Remove(id);
        return ids;
    }

    sealed record Entry(
        int Id,
        TagOutput Source,
        string Name,
        string Subclassifier,
        string Label,
        List<string> Parents,
        bool ParentsAbsent);
}
