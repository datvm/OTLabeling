namespace WebClient.Data;

public sealed class TopicCatalog(LabelingDatabase database)
{
    public IReadOnlyList<TopicRow> Topics { get; private set; } = [];
    public bool Loaded { get; private set; }

    readonly Dictionary<int, List<VerseTopicChip>> chipsByVerse = [];
    readonly Dictionary<string, Dictionary<string, HashSet<int>>> versesByCode = new(StringComparer.OrdinalIgnoreCase);

    public async Task LoadAsync()
    {
        if (Loaded)
            return;

        Topics = (await database.GetTopicsAsync()).ToList();
        foreach (var answer in await database.GetVerseTopicAnswersAsync())
        {
            if (!versesByCode.TryGetValue(answer.Code, out var byAnswer))
            {
                byAnswer = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
                versesByCode[answer.Code] = byAnswer;
            }

            if (!byAnswer.TryGetValue(answer.Answer, out var verses))
            {
                verses = [];
                byAnswer[answer.Answer] = verses;
            }

            verses.Add(answer.VerseId);
            if (answer.Answer is not ("Yes" or "Ambiguous"))
                continue;

            if (!chipsByVerse.TryGetValue(answer.VerseId, out var chips))
            {
                chips = [];
                chipsByVerse[answer.VerseId] = chips;
            }

            chips.Add(new VerseTopicChip
            {
                Code = answer.Code,
                Title = answer.Title,
                Question = answer.Question,
                Answer = answer.Answer,
                Probability = answer.Probability,
            });
        }

        Loaded = true;
    }

    public IReadOnlyList<VerseTopicChip> ChipsFor(int verseId) =>
        chipsByVerse.TryGetValue(verseId, out var chips) ? chips : [];

    public bool Matches(int verseId, IReadOnlyDictionary<string, string> constraints)
    {
        foreach (var (code, answer) in constraints)
        {
            if (!versesByCode.TryGetValue(code, out var byAnswer)
                || !byAnswer.TryGetValue(answer, out var verses)
                || !verses.Contains(verseId))
                return false;
        }

        return true;
    }

    public HashSet<int>? MatchingVerseIds(IReadOnlyDictionary<string, string> constraints)
    {
        if (constraints.Count == 0)
            return null;

        HashSet<int>? matched = null;
        foreach (var (code, answer) in constraints)
        {
            HashSet<int> verses = [];
            if (versesByCode.TryGetValue(code, out var byAnswer) && byAnswer.TryGetValue(answer, out var found))
                verses = found;

            if (matched is null)
                matched = new HashSet<int>(verses);
            else
                matched.IntersectWith(verses);
        }

        return matched;
    }
}
