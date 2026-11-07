using System.Globalization;

namespace WebClient.Data;

public sealed class TagParentGroup
{
    public IReadOnlyList<VerseTagRef> Parents { get; init; } = [];
    public IReadOnlyList<VerseTagRef> Leaves { get; init; } = [];
}

public static class TagParentGroups
{
    public static List<TagParentGroup> Build(
        IReadOnlyList<List<VerseTagRef>> chains,
        TagIndex index,
        IReadOnlyDictionary<int, VerseTagRow> tagsById)
    {
        var groups = new List<GroupBuilder>();
        var byKey = new Dictionary<string, GroupBuilder>(StringComparer.Ordinal);
        foreach (var chain in chains)
        {
            if (chain.Count == 0)
                continue;

            var leaf = chain[^1];
            var paths = ParentPaths(leaf.Id, index, tagsById);
            if (paths.Count == 0)
                paths.Add([]);

            foreach (var parents in paths)
            {
                var key = parents.Count == 0
                    ? "leaf:" + leaf.Id.ToString(CultureInfo.InvariantCulture)
                    : string.Join('>', parents.Select(parent => parent.Id.ToString(CultureInfo.InvariantCulture)));
                if (!byKey.TryGetValue(key, out var group))
                {
                    group = new GroupBuilder(parents);
                    byKey.Add(key, group);
                    groups.Add(group);
                }

                if (group.Leaves.All(item => item.Id != leaf.Id))
                    group.Leaves.Add(leaf);
            }
        }

        return groups.ConvertAll(group => new TagParentGroup
        {
            Parents = group.Parents,
            Leaves = group.Leaves,
        });
    }

    static List<List<VerseTagRef>> ParentPaths(
        int id,
        TagIndex index,
        IReadOnlyDictionary<int, VerseTagRow> tagsById)
    {
        var results = new List<List<VerseTagRef>>();
        Walk(id, [], [], results, index, tagsById);
        return results;
    }

    static void Walk(
        int id,
        List<VerseTagRef> stack,
        HashSet<int> trail,
        List<List<VerseTagRef>> results,
        TagIndex index,
        IReadOnlyDictionary<int, VerseTagRow> tagsById)
    {
        var parents = index.ParentsOf(id);
        if (parents.Count == 0)
        {
            if (stack.Count == 0)
                return;

            var path = stack.ToList();
            path.Reverse();
            results.Add(path);
            return;
        }

        foreach (var parentId in parents)
        {
            if (!trail.Add(parentId) || !tagsById.TryGetValue(parentId, out var row))
                continue;

            stack.Add(new VerseTagRef
            {
                Id = row.Id,
                Name = row.Name,
                Subclassifier = row.Subclassifier,
            });
            Walk(parentId, stack, trail, results, index, tagsById);
            stack.RemoveAt(stack.Count - 1);
            trail.Remove(parentId);
        }
    }

    sealed class GroupBuilder(IReadOnlyList<VerseTagRef> parents)
    {
        public IReadOnlyList<VerseTagRef> Parents { get; } = parents;
        public List<VerseTagRef> Leaves { get; } = [];
    }
}
