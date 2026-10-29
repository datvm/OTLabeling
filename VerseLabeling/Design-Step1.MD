# Step 1: audience tags

This file defines the step `label-is-israelites`.
Read `Design.MD` for the config, the error rule, the AI client, the database, and the log folder.
This step reads `BibleBook` and `BibleVerse`.
This step writes `LabelRun`, `LabelRunTag`, `VerseTag`, and `VerseTagMapping`.
This step does not write `VerseTagResolution`.
This step writes a log.

The role tag names are Israelite, Non-Israelite, and Everyone.
Unknown is the group name for a non-Israelite group that the model cannot name.
Use these spellings in the prompt, the check, and the log.

## What this step asks

The program sends one chapter in each AI request.
The prompt shape in this file is the whole request.
The program reads the verses from the database for one book and one chapter.
The program orders those verses by `VerseNumber`.
The program copies `BibleVerse.Text` into the chapter block.
The program writes one line for each verse.
The line is the verse number, one space, and the verse text.
The program replaces a line break in the verse text with a space.
The program does not write a blank line in the chapter block.
The program does not choose a translation.

## Summary chain

`Design.MD` defines `Summary` and `ImmediateContext` in the config.
The first request of the run uses those config values.
When a value is missing, that field in the first request is the text `None`.
A blank value also means the text `None`.

The next request in the same book uses the previous output in this run.
The program copies the previous SummarySoFar output into that request.
The program copies the previous ImmediateContext output into that request.

The model writes a new SummarySoFar.
That text covers the book through the current chapter.
The program stores that text for the next chapter.
The program does not add the previous SummarySoFar to the new text.

The model writes a new ImmediateContext for the next chapter.
The program stores that text for the next chapter.
The program does not add the previous ImmediateContext to the new text.

ImmediateContext tells the next request the current topic.
EXAMPLE: several recent chapters share one topic.
The next request needs that fact.
The count of chapters in that example is not a rule.
The model chooses what to keep.
The program does not select a fixed chapter count.

The first request of each later book uses the text `None` for SummarySoFar.
The first request of each later book uses the text `None` for ImmediateContext.
The program does not copy SummarySoFar from the previous book.
The program does not copy ImmediateContext from the previous book.

## Definitions

The program copies the definition text into the request.
The last copied line is the last line under `How to write ImmediateContext`.
The copy includes the first line, the last line, and every line between them.
The copy includes the heading lines in that span.
The program copies those lines as they are in this file.
The program does not rewrite the definition text.
The copy does not include `Group names`.
The first copied line is the next paragraph.

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

Use Everyone only when the verse speaks of people in general and does not refer to any identifiable group.
When Everyone is present it must be the only tag in the array.
A verse that states a general principle but also refers to a specific group receives the specific tag(s), not Everyone.

Example: Psalm 34:15 says, “The eyes of the LORD are on the righteous.” The verse does not name Israelites, a foreign nation, or any other identifiable group.
When Everyone is present, it is the only tag on that verse.

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

### How to choose the tags

A verse has a `Tags` array.
The array can contain more than one tag.
Include Israelite when the verse refers to Israelites.
Include Non-Israelite when the verse refers to a group that is not Israelite.
Israelite and Non-Israelite may both appear on one verse.
Include each specific group name that the verse refers to.
Also include Non-Israelite when the array contains a specific non-Israelite group name.
Do not include Everyone when the tags name specific groups.
Write each tag name once.
You may write a group name that this text does not list.

EXAMPLE: a judgment on Egypt and Cush has the tags Non-Israelite, Egypt, and Cushite.
EXAMPLE: Israelites who fight Egypt and Cush have the tags Israelite, Non-Israelite, Egypt, and Cush.
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

## Group names

The model may write any specific group name.
Step 1 stores that name as the model wrote it.
Step 1 does not merge names.
A later step can merge names that refer to one group.
EXAMPLE: Egypt and Egyptian may become one tag in that later step.
That pair is an example. It is not a required merge.

Do not start a full run until the definition text in this file exists.

## Output rules

The program copies the output rules into the request after the definition text.
The last copied line is the closing fence of the JSON example.
The copy includes the first line, the last line, and every line between them.
The program copies those lines as they are in this file.
The program does not rewrite the output rules.
The first copied line is the next paragraph.

The model returns one JSON object.
The property names are PascalCase.
The object has `Verses`, `SummarySoFar`, and `ImmediateContext`.

Each item in `Verses` has `VerseNumber` and `Tags`.
`Tags` is an array of tag names.
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
      "Tags": ["Israelite", "Non-Israelite", "Egypt", "Cush"]
    }
  ],
  "SummarySoFar": "{text}",
  "ImmediateContext": "{text}"
}
```

## Prompt shape

The program builds the request in this shape.
`{definition text}` is the copy defined under Definitions.
`{output rules}` is the copy defined under Output rules.
The program does not write those rules a second time in this shape.

```text
{definition text}

{output rules}

SummarySoFar:
{SummarySoFar or None}

ImmediateContext:
{ImmediateContext or None}

Chapter:
{verse number} {verse text}
```

## Checks

The program checks the object before a success log and before the database write for that chapter.
`Design.MD` defines the error rule.
The program uses that rule when the call fails, when the body is not one JSON object, when a JSON value has the wrong type, and when this check fails.
The program matches the property names in PascalCase only.
The program accepts these property names: `Verses`, `VerseNumber`, `Tags`, `SummarySoFar`, and `ImmediateContext`.
The program ignores any other JSON property.

The fixed names are Israelite, Non-Israelite, Everyone, and Unknown.
The check fails when a tag differs from a fixed name only by letter case.
Any other spelling stays as a group name.
EXAMPLE: Israelites stays as a group name.
That word is an example. It is not a required tag.
The program stores that group name as the model wrote it.
The check fails in these cases:

- A verse from the chapter is absent.
- A verse number repeats.
- A verse number is not in the chapter.
- `Tags` is absent.
- `Tags` is empty.
- A tag name is blank.
- A tag name is only spaces.
- A tag name repeats in that verse.
- Everyone shares the array with another tag.
- `SummarySoFar` has no words.
- `ImmediateContext` has no words.

A failed check does not write that chapter to `verses.csv`.
A failed check does not write that chapter to `chapters.jsonl`.
A failed check does not write that chapter to `LabelRunTag` or `VerseTagMapping`.
A failed check does not create a `VerseTag` from that chapter.

## Chapter order

The program reads the books in the `Books` array order.
The program completes one book before the next book.
Inside a book, the program sends the selected chapters in ascending order.
When the check succeeds, the program writes that chapter to the database.
`Design.MD` defines the run result rows and the real-tag update.
The program appends that chapter to the log after the database transaction commits.
The program sends the next request after that append.

## Log files

The run folder is `out/verse-labeling/label-is-israelites/{run-id}/`.
`Design.MD` defines `{run-id}`.

The program writes these files:

- `run.txt` for the run settings.
- `verses.csv` for one row per tag.
- `chapters.jsonl` for one object per chapter.
- `raw/{bookCode}-{chapter}.txt` for the raw response text.

`{bookCode}` is `BibleBook.Code`.

The program writes `run.txt` when it inserts the `LabelRun` row.
`run.txt` lists these fields:

- the step
- the book selection
- the chapter selection
- the request limit
- the base URL
- the model name
- the UTC start

`run.txt` does not contain the API key.

Example of `run.txt`. The books, the range, and the time are examples.
The request limit in the example is the default.
When the config selects every chapter, the chapter line is `chapters=all`.

```text
step=label-is-israelites
books=GEN,EXO
chapters=5-
rpm=15
baseUrl={base URL}
model={model name}
startedUtc=20261026T160800Z
```

`verses.csv` uses UTF-8.
The file has a header row.
Columns: `BookCode`, `Chapter`, `VerseNumber`, `TagIndex`, `Tag`.
`TagIndex` starts at 1.
`TagIndex` follows the order of the `Tags` array.
One verse with four tags has four rows.
Row order is the `Books` array order, then chapter, then verse number, then `TagIndex`.

`chapters.jsonl` uses one JSON object per line.
Fields: `BookCode`, `Chapter`, `SummarySoFar`, `ImmediateContext`.
JSONL keeps commas and line breaks inside the two texts.

The program writes `raw/{bookCode}-{chapter}.txt` for every response body.
The program writes `verses.csv` and `chapters.jsonl` only after the check passes and the database transaction commits.

## Values to supply

No open values remain in this file.
