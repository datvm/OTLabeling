namespace VerseLabeling.Services;

static class ChapterPrompt
{
    const string Rules = """
        Use only the chapter text supplied in this request, plus the SummarySoFar and ImmediateContext that this request supplies.
        Do not add people, events, or places that those texts do not mention.
        You may use the definition of the Promised Land in these rules to decide whether a named place is inside that land.
        Spell People, Places, Israelite, Non-Israelite, Pre-Israelite, Everyone, Unknown, Antediluvian, and Non-Israel exactly as written in this request.

        People and Places are the root tags.
        A root tag has an empty Parents array.
        A tag may have more than one parent.
        Every parent is another tag in the same Tags array.
        Follow parents until you reach a root tag.
        The verse Tags array lists each person, people, and place the verse refers to.
        A verse may list a person and a place together.
        A verse may list a tag and its parent when the verse refers to both.
        Example: Babylon is in Shinar. A verse that refers to both lists Babylon and Shinar.
        Shinar is the parent of Babylon.
        A parent that the verse does not name still belongs to the verse through the Tags array.
        A person stays on the People chain.
        A place stays on the Places chain.
        The same name may be one person and one place.
        Those tags have different roots, so leave Subclassifier empty.
        When one name is two tags under the same root, give each one a Subclassifier, a short phrase from the supplied text.
        Leave Subclassifier empty when the name is only one tag.
        A verse tag is the name.
        When the tag has a Subclassifier, write the verse tag as Name (Subclassifier).
        When a verse uses a name that is under more than one root, also list a parent of the tag you mean.
        Example: Canaan the son of Ham has an empty Subclassifier and the parent Pre-Israelite.
        Canaan the land has an empty Subclassifier and the parent Non-Israel.
        A verse about the son lists Canaan and Pre-Israelite.
        A verse about the land lists Canaan and Non-Israel.
        Use that same Subclassifier when a later chapter mentions that same person or place.

        ### People

        People is a root tag.
        These tags are children of People, and People is their only parent: Israelite, Non-Israelite, Pre-Israelite, Everyone, Unknown, and Antediluvian.

        Israelite is a person or group in the line of promise that begins with Abraham in Genesis 12 and continues through Isaac, Jacob, and their descendants.
        Use Israelite only from Genesis 12 onward.
        Do not use Israelite before Genesis 12.
        Treat "Hebrew," "children of Israel," "house of Jacob," and similar expressions as Israelite when the chapter text or the SummarySoFar supplied in the request makes the identification clear.
        When the verse names a person or a more specific group in that line, that name is the verse tag, and Israelite is its parent.
        The name Israel can be that person. Jacob receives the name Israel. That tag's parent is Israelite.

        Non-Israelite is a person or group that the chapter text identifies as outside the Israelite line.
        Use Non-Israelite only from Genesis 12 onward.
        Do not use Non-Israelite before Genesis 12.
        When the text names a specific people, that people name is a tag and Non-Israelite is its parent.
        The verse then lists the people name, not Non-Israelite.
        Non-Israelite is the people tag. Non-Israel is the place tag. They are different tags.

        Pre-Israelite is a person or group in the text before Genesis 12.
        Use Pre-Israelite only in Genesis chapters 1 through 11.
        Do not use Pre-Israelite from Genesis 12 onward.
        When the verse names a person or a people, that name is the verse tag, and Pre-Israelite is its parent.
        Do not use Israelite or Non-Israelite for that person or people.

        Everyone means the verse speaks of people in general.
        Everyone may be the only people tag on the verse.
        Everyone may share the verse with another tag when the verse also refers to a specific person, people, or place.
        Example: Genesis 12:3 refers to Abraham and also to all the families of the earth.
        Abraham has the parent Israelite.
        The verse lists Abraham and Everyone.

        Example: Psalm 34:15 says, "The eyes of the LORD are on the righteous."
        The verse does not name a people or a place.
        That verse lists Everyone.

        Unknown is for a group of people that the supplied chapter, SummarySoFar, and ImmediateContext never name and never place.
        From Genesis 12 onward, the verse lists Unknown and Non-Israelite when that group is outside the Israelite line.
        Before Genesis 12, the verse lists Unknown and Pre-Israelite.
        A place of origin is enough. Do not use Unknown when the supplied text says where the group is from.
        Example: the shepherds in Genesis 29 say that they are from Haran.
        Haran is a place. Its parent is Non-Israel.
        The shepherds are Non-Israelite.
        Those verses list Haran, and they do not list Unknown.
        Use that identification for every verse in the chapter that refers to those shepherds, including the verses before the sentence that names Haran.
        A servant, steward, or attendant of a named person is not Unknown.
        Example: Genesis 15 speaks of a nation that the supplied text does not name and does not place. That nation is Unknown.

        Antediluvian is a person or group that the supplied text places before the flood.
        It may be a second parent beside Pre-Israelite.
        Example: Lamech has the parents Pre-Israelite and Antediluvian.
        The verse lists Lamech only.

        Include a tag for each specific person the verse refers to.
        Use that person's name.
        A verse may refer to more than one person.
        Include each of those names when neither name is a parent of the other.
        Example: a verse that refers to Abraham and Lot lists Abraham and Lot.
        Abraham has the parent Israelite.
        Lot has the parent Non-Israelite when the supplied text places him outside the Israelite line.
        Those names are examples. They are not a required list.
        Do not add a person that the supplied text does not mention.

        ### Places

        Places is a root tag.
        Non-Israel is its child. Places is Non-Israel's only parent.
        Non-Israel is a place outside the Promised Land Joshua entered, and every place before that land is given to Joshua.
        This includes the land the spies enter.
        A named place has the nearer place as its parent when the supplied text gives one.
        The verse lists the most specific place.
        Example: in Genesis 47, Goshen is inside Egypt, and the land has not been given to Joshua.
        Non-Israel is the parent of Egypt.
        Egypt is the parent of Goshen.
        The verse lists Goshen.
        When the text uses Israel for the land, Israel is a place tag and Places is its parent.

        ### How to choose the tags

        Put every tag used by the chapter into the Tags array, including each parent, up to a root tag.
        Each item has Name and Parents.
        Parents is an array of tag names.
        People and Places have empty Parents arrays.
        Israelite, Non-Israelite, Pre-Israelite, Everyone, Unknown, and Antediluvian have the parent People.
        Non-Israel has the parent Places.
        Any other tag has one or more parents.
        Write each tag name once in Tags.
        A people name, a person name, or a place name may be absent from this instruction.
        It must appear in the supplied chapter, SummarySoFar, or ImmediateContext.

        Each verse has its own Tags array of names.
        That array lists each person, people, and place the verse refers to.
        The array may include a tag and its parent.
        The array is empty when the verse refers to no person, no people, and no place.

        Example: Ruth belongs to Israel and to Moab.
        Moabite has the parent Non-Israelite.
        Ruth has the parents Israelite and Moabite.
        The verse lists Ruth only.

        Example: a verse before Genesis 12 that names Japheth lists Japheth.
        Japheth has the parent Pre-Israelite.
        These examples show the shape. They are not required test verses.

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
        The object has Tags, Verses, SummarySoFar, and ImmediateContext.

        Tags lists each tag used in the chapter.
        Each item has Name and Parents.
        Verses includes every verse of the chapter exactly once.
        Each item has VerseNumber and Tags.
        The verse Tags array lists the tag names for that verse.
        An empty array means the verse refers to no person, no people, and no place.

        SummarySoFar is the book summary through this chapter.
        ImmediateContext is the topic text for the next chapter.
        Both texts must contain words.

        Example of the object shape.
        The names in it are examples.

        ```json
        {
          "Tags": [
            { "Name": "People", "Parents": [] },
            { "Name": "Places", "Parents": [] },
            { "Name": "Israelite", "Parents": ["People"] },
            { "Name": "Non-Israel", "Parents": ["Places"] },
            { "Name": "Egypt", "Parents": ["Non-Israel"] },
            { "Name": "Goshen", "Parents": ["Egypt"] },
            { "Name": "Jacob", "Parents": ["Israelite"] }
          ],
          "Verses": [
            { "VerseNumber": 1, "Tags": ["Jacob", "Goshen"] }
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
