namespace VerseLabeling.Services;

static class ChapterPrompt
{
    const string Rules = """
        Use only the chapter text supplied in this request, plus the SummarySoFar and ImmediateContext that this request supplies.
        Do not add people, events, or places that those texts do not mention.
        You may use the definition of the Promised Land in these rules to decide whether a named place is inside that land.
        Spell People, God, Places, Israelite, Non-Israelite, Pre-Israelite, Antediluvian, Others, Israel, and Non-Israel exactly as written in this request.
        Different capital letters do not make a new tag.
        LORD and Lord are the same tag.
        Lord God and Lord GOD are the same tag.
        Do not use Everyone.
        Do not use Unknown. Use Others.

        Each verse tag is one path.
        A path starts with People, God, or Places.
        A dot separates each step.
        Do not return a Tags catalog.
        Do not return Name, Parents, or Subclassifier.
        The path is the whole declaration.
        Example: People.Non-Israelite.Moabite.
        Example: People.Others.
        Example: Places.Non-Israel.Egypt.Goshen.
        Example: God.LORD.
        Example: God.
        A root is a whole path only when that root has no fixed children.
        God has no fixed children. God himself is the path God, not God.God.
        People and Places have fixed children, so People alone and Places alone are not paths.
        Use a leaf fixed tag when the text names nothing more specific.
        The leaf fixed tags are People.Antediluvian, People.Pre-Israelite, People.Israelite, People.Non-Israelite, People.Others, Places.Israel, Places.Non-Israel, and Places.Others.
        When no people category can be determined, use People.Others.
        When no place category can be determined, use Places.Others.
        Do not repeat a step.
        The land is Places.Israel, not Israel.Israel and not Places.Israel.Israel.
        The person Israel is People.Israelite.Israel. Stop there when the text names no further person.
        The children of People are Antediluvian, Pre-Israelite, Israelite, Non-Israelite, and Others.
        A person or a people is not a direct step under People.
        People.Others has no further step.
        The children of Places are Israel, Non-Israel, and Others.
        A place is not a direct step under Places.
        Places.Others has no further step.
        A name of God is a step under God, such as God.LORD, God.Lord God, or God.LORD of Hosts.
        Do not put God, or a name of God, under People.
        Israel the person is People.Israelite.Israel.
        Israel the land is Places.Israel.
        Those are two paths. Do not add a classifier to either name.
        Canaan the son of Ham is People.Pre-Israelite.Canaan.
        Canaan the land is Places.Non-Israel.Canaan.
        A verse about the son lists the person path.
        A verse about the land lists the place path.
        A Subclassifier is the phrase in parentheses on the last step.
        Leave the Subclassifier off.
        A Subclassifier is minimal, and only when absolutely necessary.
        It is necessary only when this chapter has two different people or places that would otherwise have the same path.
        A title, a description, or a poetic phrase is not a reason to add one.
        A wife, son, family, or other companion is not a Subclassifier.
        That person or group gets its own tag name.
        Noah's wife is People.Antediluvian.Noah's wife, not Noah (wife).
        Noah's sons are People.Antediluvian.Noah's sons, not Noah (sons).
        Daughter of Zion is People.Israelite.Zion.
        The Holy One of Israel is not a Subclassifier on Israel.
        God himself is the path God.
        Do not write God.God.
        Do not repeat Israel. The land is Places.Israel. The person is People.Israelite.Israel.
        When a Subclassifier is necessary, write the last step as Name (phrase).
        The phrase is the shortest words that tell the two apart, at most 40 characters.
        Example: People.Pre-Israelite.Lamech (father of Noah) and People.Pre-Israelite.Lamech (son of Noah).
        A fixed step does not take a Subclassifier.
        The fixed steps are People, God, Places, Antediluvian, Pre-Israelite, Israelite, Non-Israelite, Others, Israel, and Non-Israel.
        Use the same path when a later chapter mentions that same person or place.

        ### People

        People is a root tag.
        These tags are children of People, and People is their only parent: Antediluvian, Pre-Israelite, Israelite, Non-Israelite, and Others.
        A person name or a people name is not a direct child of People.
        Its parent is one of those five tags, or a more specific tag under one of them.

        Israelite is a person or group in the line of promise that begins with Abraham in Genesis 12 and continues through Isaac, Jacob, and their descendants.
        Use Israelite only from Genesis 12 onward.
        Do not use Israelite before Genesis 12.
        Treat "Hebrew," "children of Israel," "house of Jacob," and similar expressions as Israelite when the chapter text or the SummarySoFar supplied in the request makes the identification clear.
        When the verse names a person or a more specific group in that line, the path continues under Israelite.
        Example: People.Israelite.Abraham.
        The name Israel can be that person. Jacob receives the name Israel.
        That path is People.Israelite.Israel.

        Non-Israelite is a person or group that the chapter text identifies as outside the Israelite line.
        Use Non-Israelite only from Genesis 12 onward.
        Do not use Non-Israelite before Genesis 12.
        When the text names a specific people, the path continues under Non-Israelite.
        Example: People.Non-Israelite.Moabite.
        When the text does not name a people, stop at People.Non-Israelite.
        Non-Israelite is the people tag. Non-Israel is the place tag. They are different tags.

        Pre-Israelite is a person or group in the text before Genesis 12.
        Use Pre-Israelite only in Genesis chapters 1 through 11.
        Do not use Pre-Israelite from Genesis 12 onward.
        When the verse names a person or a people, the path continues under Pre-Israelite.
        Example: People.Pre-Israelite.Japheth.
        Do not use Israelite or Non-Israelite for that person or people.

        People.Others is a group that the supplied chapter, SummarySoFar, and ImmediateContext never name and never place.
        From Genesis 12 onward, the verse lists People.Others and People.Non-Israelite when that group is outside the Israelite line.
        Before Genesis 12, the verse lists People.Others and People.Pre-Israelite.
        A place of origin is enough. Do not use People.Others when the supplied text says where the group is from.
        Example: the shepherds in Genesis 29 say that they are from Haran.
        Haran is Places.Non-Israel.Haran.
        The shepherds are People.Non-Israelite.
        Those verses list Places.Non-Israel.Haran, and they do not list People.Others.
        Use that identification for every verse in the chapter that refers to those shepherds, including the verses before the sentence that names Haran.
        A servant, steward, or attendant of a named person is not Others.
        Example: Genesis 15 speaks of a nation that the supplied text does not name and does not place. That nation is People.Others.
        Example: Genesis 12:3 refers to Abraham and also to all the families of the earth.
        Abraham is People.Israelite.Abraham.
        The families are People.Others.
        The verse lists People.Israelite.Abraham and People.Others.
        Example: Psalm 34:15 says, "The eyes of the LORD are on the righteous."
        The verse does not name a people or a place.
        That verse lists People.Others and God.LORD.

        Antediluvian is a person or group that the supplied text places before the flood.
        Example: People.Antediluvian.Lamech.
        A person uses one path. Do not also write a Pre-Israelite path for that same person.

        Include a tag for each specific person the verse refers to.
        Use that person's name.
        A verse may refer to more than one person.
        Include each of those names when neither name is a parent of the other.
        Example: a verse that refers to Abraham and Lot lists People.Israelite.Abraham and People.Non-Israelite.Lot.
        Lot uses Non-Israelite when the supplied text places him outside the Israelite line.
        Those names are examples. They are not a required list.
        Do not add a person that the supplied text does not mention.

        ### God

        God is a root path.
        A name or title of God is the next step.
        Examples: God.LORD, God.Lord God, God.LORD of Hosts.
        Those names are examples. They are not a required list.
        Do not put God, or a name of God, under People.

        ### Places

        Places is a root path.
        Israel, Non-Israel, and Others are its children.
        A named place is not a direct step under Places.
        Israel, the place, is the Promised Land Joshua entered, either the whole land or a part of it.
        When the text uses Israel for that land, the path is Places.Israel.
        When the text uses Israel for Jacob, the path is People.Israelite.Israel.
        Non-Israel is a place outside the Promised Land Joshua entered, and every place before that land is given to Joshua.
        This includes the land the spies enter.
        Places.Others is a place that is unknown, not classifiable, or irrelevant.
        A named place continues under the nearer place when the supplied text gives one.
        The verse lists the most specific path.
        Example: in Genesis 47, Goshen is inside Egypt, and the land has not been given to Joshua.
        The verse lists Places.Non-Israel.Egypt.Goshen.

        ### How to choose the tags

        Each verse has its own Tags array of paths.
        That array lists each person, people, place, and name of God the verse refers to.
        The array is empty when the verse refers to no person, no people, no place, and no name of God.
        A people name, a person name, or a place name may be absent from this instruction.
        It must appear in the supplied chapter, SummarySoFar, or ImmediateContext.
        When the verse refers to a place and the people from that place, list both paths.
        Example: Babylon is in Shinar. A verse that refers to both lists Places.Non-Israel.Shinar.Babylon and Places.Non-Israel.Shinar.

        Example: Ruth belongs to Israel and to Moab.
        The verse lists People.Israelite.Ruth and People.Non-Israelite.Moabite.

        Example: a verse before Genesis 12 that names Japheth lists People.Pre-Israelite.Japheth.
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
        The object has Verses, SummarySoFar, and ImmediateContext.
        Do not include a Tags catalog.

        Verses includes every verse of the chapter exactly once.
        Each item has VerseNumber and Tags.
        The verse Tags array lists the paths for that verse.
        An empty array means the verse refers to no person, no people, no place, and no name of God.

        SummarySoFar is the book summary through this chapter.
        ImmediateContext is the topic text for the next chapter.
        Both texts must contain words.

        Example of the object shape.
        The names in it are examples.

        ```json
        {
          "Verses": [
            { "VerseNumber": 1, "Tags": ["People.Israelite.Jacob", "Places.Non-Israel.Egypt.Goshen"] }
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
