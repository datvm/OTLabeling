namespace VerseLabeling.Services;

static class ChapterPrompt
{
    const string Rules = """
        Use only the chapter text supplied in this request, plus the SummarySoFar and ImmediateContext that this request supplies.
        Do not add facts from other biblical books, from later history, or from your training data.
        Spell Israelite, Non-Israelite, Everyone, Unknown, and Assimilated exactly as written in this request.

        ### Israelite

        A person or group that belongs to the line of promise that begins with Abraham (Genesis 12) and continues through Isaac, Jacob, and their descendants.
        Do not apply the Israelite tag before Genesis 12.
        Treat "Hebrew," "children of Israel," "house of Jacob," and similar expressions as Israelite when the chapter text or the SummarySoFar supplied in the request makes the identification clear.
        Do not apply the tag on the basis of outside knowledge.

        ### Non-Israelite

        Any person or group that the chapter text identifies as outside the Israelite line.
        When the text names a specific group, include that group name and also include Non-Israelite.
        When the text refers to a non-Israelite group that cannot be named from the supplied text, include both Non-Israelite and Unknown.

        ### Everyone

        Use Everyone when the verse speaks of people in general.
        Everyone may be the only tag.
        Everyone may also share the array with other tags when the verse refers to a specific group as well.
        Example: Genesis 12:3 refers to Abraham and also to all the families of the earth. That verse receives Israelite, Abraham, and Everyone.

        Example: Psalm 34:15 says, “The eyes of the LORD are on the righteous.” The verse does not name Israelites, a foreign nation, or any other identifiable group.
        That verse receives Everyone alone.

        ### Unknown

        Use Unknown (together with Non-Israelite) when the verse refers to a non-Israelite group and the supplied text does not give a name for that group.

        The model writes Unknown when the verse refers to a non-Israelite group and the model cannot name that group.
        The model also writes Non-Israelite on that verse.

        ### Assimilated

        A free-form group name that always appears together with the Israelite role tag.
        Use it for a person or group who was not originally Israelite but who is described in the supplied text as joining or becoming part of the Israelite community.
        Also include the original group name when the text gives one.
        Example: Ruth is a Moabite who joins the community of Israel. A verse that describes her as doing so receives the tags `["Israelite", "Assimilated", "Moabite"]`.
        Do not use Assimilated as a substitute for Israelite.
        Do not use Assimilated when the text does not describe the person or group as joining the community.

        ### Person

        Include a tag for each specific person the verse refers to.
        Use that person's name.
        A verse may refer to more than one person.
        Include each of those names.
        Example: a verse that refers to Abraham and Lot receives Abraham and Lot.
        Those names are examples. They are not a required list.
        Do not add a person from outside knowledge.
        Use only the chapter text, the SummarySoFar, and the ImmediateContext supplied in this request.

        ### How to choose the tags

        A verse has a `Tags` array.
        The array can contain more than one tag.
        The array is empty when the verse refers to no person and no group.
        Include Israelite when the verse refers to Israelites.
        Include Non-Israelite when the verse refers to a group that is not Israelite.
        Israelite and Non-Israelite may both appear on one verse.
        Include each specific group name that the verse refers to.
        Also include Non-Israelite when the array contains a specific non-Israelite group name.
        Include each specific person name that the verse refers to.
        A verse may include more than one person name.
        Include Everyone when the verse speaks of people in general.
        Everyone may share the array with other tags.
        Write each tag name once.
        You may write a group name that this text does not list.
        You may write a person name that this text does not list.

        EXAMPLE: a judgment on Egypt and Cush has the tags Non-Israelite, Egypt, and Cushite.
        EXAMPLE: Israelites who fight Egypt and Cush have the tags Israelite, Non-Israelite, Egypt, and Cush.
        EXAMPLE: a verse that refers to Abraham and Lot has the tags Abraham and Lot.
        These examples show the array. They are not required test verses.
        The names in the examples are not a fixed list.

        ### How to write SummarySoFar

        Write a new summary of this book through the current chapter.
        Maximum 150 words.
        Include the current chapter.
        Do not copy the previous SummarySoFar verbatim.
        Use only information present in the chapter text and in the SummarySoFar that was supplied in this request.

        ### How to write ImmediateContext

        Write a short note (maximum 60 words) that tells the next request what the current topic is.
        Use only information present in the chapter text and in the ImmediateContext that was supplied in this request.

        The model returns one JSON object.
        The property names are PascalCase.
        The object has `Verses`, `SummarySoFar`, and `ImmediateContext`.

        Each item in `Verses` has `VerseNumber` and `Tags`.
        `Tags` is an array of tag names.
        Include each specific person name in that array.
        A verse may include more than one person name.
        An empty array means the verse refers to no person and no group.
        The model includes every verse of the chapter exactly once.
        The rules in "How to choose the tags" decide the array.

        `SummarySoFar` is the book summary through this chapter.
        `ImmediateContext` is the topic text for the next chapter.
        Both texts must contain words.

        Example of the object shape.
        The verse number and the tag names in it are examples.

        ```json
        {
          "Verses": [
            {
              "VerseNumber": 1,
              "Tags": ["Israelite", "Abraham", "Non-Israelite", "Lot"]
            }
          ],
          "SummarySoFar": "{text}",
          "ImmediateContext": "{text}"
        }
        ```
        """;

    public static string Build(string summary, string immediateContext, IReadOnlyList<BibleVerse> verses)
    {
        var builder = new StringBuilder();
        builder.Append(Rules);
        builder.Append("\n\nSummarySoFar:\n");
        builder.Append(summary);
        builder.Append("\n\nImmediateContext:\n");
        builder.Append(immediateContext);
        builder.Append("\n\nChapter:\n");
        foreach (var verse in verses)
        {
            builder.Append(verse.VerseNumber);
            builder.Append(' ');
            builder.Append(Flatten(verse.Text));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    static string Flatten(string text) =>
        text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
}
