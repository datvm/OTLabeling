using System.Globalization;

namespace VerseLabeling.Services;

static class TopicPrompt
{
    const string Rules = """
        Use only `chapter`, and `previous` when this request includes it.
        When `previous` is present, `previous.summarySoFar` is the book summary through the previous chapter, and `previous.immediateContext` is the topic of that chapter.
        Do not add people, nations, places, or events that those texts do not mention.
        Each question names one verse in `chapter`.
        Answer that question for that verse.
        The other verses in `chapter`, and `previous` when it is present, may show who or what that verse refers to.
        That includes a pronoun.
        A nation, people, or group counts only when `chapter` or `previous` names it.
        """;

    public static string Key(int verseNumber, int sortOrder) =>
        "v" + verseNumber.ToString(CultureInfo.InvariantCulture) + "t" + sortOrder.ToString(CultureInfo.InvariantCulture);

    public static TopicRequest Build(
        string? summary,
        string? immediate,
        IReadOnlyList<BibleVerse> verses,
        IReadOnlyList<RunTopic> topics)
    {
        TopicPrevious? previous = null;
        if (summary is not null && immediate is not null)
        {
            previous = new TopicPrevious
            {
                SummarySoFar = summary,
                ImmediateContext = immediate,
            };
        }

        var chapter = new List<TopicVerseLine>(verses.Count);
        var questions = new Dictionary<string, NoulQuestion>(verses.Count * topics.Count);
        foreach (var verse in verses)
        {
            chapter.Add(new TopicVerseLine
            {
                VerseNumber = verse.VerseNumber,
                Text = Flatten(verse.Text),
            });
            foreach (var topic in topics)
            {
                var instruction = "For verse " + verse.VerseNumber.ToString(CultureInfo.InvariantCulture) + " in `chapter`: " + topic.Question;
                questions[Key(verse.VerseNumber, topic.SortOrder)] = new NoulQuestion(instruction);
            }
        }

        return new TopicRequest(
            new TopicState
            {
                Rules = Rules,
                Previous = previous,
                Chapter = chapter,
            },
            questions);
    }

    static string Flatten(string text) =>
        text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
}

sealed record TopicRequest(TopicState State, Dictionary<string, NoulQuestion> Questions);

sealed class TopicState
{
    public required string Rules { get; init; }
    public TopicPrevious? Previous { get; init; }
    public required List<TopicVerseLine> Chapter { get; init; }
}

sealed class TopicPrevious
{
    public required string SummarySoFar { get; init; }
    public required string ImmediateContext { get; init; }
}

sealed class TopicVerseLine
{
    public required int VerseNumber { get; init; }
    public required string Text { get; init; }
}

sealed class NoulQuestion(string instructions)
{
    public string Type { get; } = "noul";
    public string Instructions { get; } = instructions;
}
