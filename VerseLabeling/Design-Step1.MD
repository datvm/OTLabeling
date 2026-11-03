# Step 1: audience tags

This file defines the step `label-is-israelites`.
Read `Design.MD` for the config, the error rule, the AI client, the database, and the log folder.
This step reads `BibleBook` and `BibleVerse`.
This step writes `LabelRun`, `LabelRunTag`, `VerseTag`, `VerseTagParent`, and `VerseTagMapping`.
This step does not write `VerseTagResolution`.
This step writes a log.

The root tags are People and Places.
Antediluvian, Pre-Israelite, Israelite, Non-Israelite, and Others are children of People.
God is a root tag. A name of God is a child of God.
Israel, Non-Israel, and Others are children of Places.
Non-Israel is a place outside the Promised Land, including every place before Joshua.
Israel the place has Places as its only parent.
Israel the person has Israelite as its parent.
A free-form tag is not a direct child of People or Places.
A person name is the name of one specific person.
A place name is the name of one specific place.
The model writes a person name and a place name.
Those names are not from a fixed list.
Use these spellings in the prompt, the check, and the log.

## What this step asks

The program sends one chapter in each AI request.
The request text is `ChapterPrompt`.
The program does not read this file to build the request.
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

## Request

The request text is `ChapterPrompt` in the VerseLabeling project.
The program does not read this file to build the request.
The program does not read the request text from `project-conf.json`.

The request has this shape.

```text
{rules}

SummarySoFar:
{SummarySoFar or None}

ImmediateContext:
{ImmediateContext or None}

Chapter:
{verse number} {verse text}
```

`{rules}` is the text in `ChapterPrompt`.
That text states the tag rules, including a tag for each named person and each named place.
`{SummarySoFar or None}` and `{ImmediateContext or None}` come from the summary chain.
`{verse number} {verse text}` is one line for each verse in the chapter.

## Stored names

The program stores a group name as the model wrote it.
The program stores a person name as the model wrote it.
The program stores a place name as the model wrote it.
The program does not merge names.

## Checks

The program checks the object before a success log and before the database write for that chapter.
`Design.MD` defines the error rule.
The program uses that rule when the call fails, when the body is not one JSON object, when a JSON value has the wrong type, and when this check fails.
The program matches the property names in PascalCase only.
The program accepts these property names: `Verses`, `VerseNumber`, `Tags`, `SummarySoFar`, and `ImmediateContext`.
A verse tag is a path.
A path starts with People, God, or Places.
A dot separates each step.
EXAMPLE: `People.Non-Israelite.Moabite`.
EXAMPLE: `People.Others`.
EXAMPLE: `Places.Non-Israel.Egypt.Goshen`.
The response does not include a tag catalog.
A root is a whole path only when that root has no fixed children.
God has no fixed children, so `God` is a valid path.
People and Places have fixed children, so those roots alone fail the check.
A leaf fixed tag is a valid path when the text names nothing more specific.
A repeated step fails the check. `God.God` is `God`. `Places.Israel.Israel` and `Israel.Israel` are `Places.Israel`.
The program ignores any other JSON property.

The root tags are People and Places.
The people tags are Antediluvian, Pre-Israelite, Israelite, Non-Israelite, and Others.
People is the only parent of each people tag.
God is a root tag.
The place tags are Israel, Non-Israel, and Others.
Places is the only parent of each place tag.
The people tag Others and the place tag Others are different tags.
Do not use Everyone.
Do not use Unknown. Use Others.
The check fails when a fixed tag differs from one of these names only by letter case.
Any other tag is the same tag when the only difference is capital letters.
LORD and Lord are one tag.
Lord God and Lord GOD are one tag.
Any other spelling stays as the model wrote it.
That spelling may be a group name, a person name, or a place name.
EXAMPLE: Israelites stays as a group name.
EXAMPLE: Abraham stays as a person name.
EXAMPLE: Goshen stays as a place name.
Those words are examples. They are not required tags.
The program stores that name as the model wrote it.
The program keeps one copy of a repeated path in a verse `Tags` array.
Two paths that differ only by capital letters are one tag on that verse.
The first copy keeps its place.
A Subclassifier is minimal, and only when absolutely necessary.
It is necessary only when two people or places in the chapter would otherwise have the same path.
The phrase is the shortest words that tell the two apart.
A title, a description, or a poetic phrase is not a reason to add one.
A wife, son, family, or other companion is a separate tag.
Noah's wife is `People.Antediluvian.Noah's wife`, not `Noah (wife)`.
An extra Subclassifier does not fail the check.
Two paths that end with the same name and share a root need a short classifier on the last step.
That classifier is at most 40 characters.
A fixed step does not take a classifier.
People.Others and Places.Others are different paths.
People.Israelite.Israel and Places.Israel are different paths.
The check fails in these cases:

- A verse from the chapter is absent.
- A verse number repeats.
- A verse number is not in the chapter.
- A verse `Tags` array is absent.
- A verse tag is blank.
- A verse tag is not a path.
- A path does not start with People, God, or Places.
- A free-form name is a direct step under People or Places.
- A path adds a child under Others.
- A locked step has a classifier. The locked steps are People, God, Places, Antediluvian, Pre-Israelite, Israelite, Non-Israelite, Others, Israel, and Non-Israel.
- A classifier is longer than 40 characters.
- Two paths end with the same name, share a root, and have no classifier.
- The tag name is Everyone or Unknown.
- Israelite or Non-Israelite appears before Genesis 12.
- Pre-Israelite appears outside Genesis 1-11.
- `SummarySoFar` has no words.
- `ImmediateContext` has no words.

A verse may include a tag and a parent of that tag.
The check allows that pair.
EXAMPLE: Babylon and Shinar may both be tags on one verse.
Shinar is the parent of Babylon.
That pair is an example. It is not a required verse.

An empty chapter `Tags` array passes the check when every verse `Tags` array is empty.
An empty verse `Tags` array passes the check.
The verse refers to no person, no people, and no place.

A failed check does not write that chapter to `verses.csv`.
A failed check does not write that chapter to `chapters.jsonl`.
A failed check does not write that chapter to `LabelRunTag`, `VerseTagParent`, or `VerseTagMapping`.
A failed check does not create a `VerseTag` from that chapter.
A failed check leaves that chapter's `LabelRunChapter.Filled` false.

## Chapter order

`Design.MD` defines `ResumeRun` and `LabelRunChapter`.
A new run inserts one `LabelRunChapter` for every selected chapter before the first request.
The program reads the books in the `Books` array order.
The program completes one book before the next book.
Inside a book, the program sends the selected chapters in ascending order.
When the check succeeds, the program writes that chapter to the database.
`Design.MD` defines the run result rows and the real-tag update.
The program appends that chapter to the log after the database transaction commits.
The program sends the next request after that append.
The program writes `{book code} {chapter} started.` before that request.
`Design.MD` defines the token and cost line written after each successful response.
The program writes `{book code} {chapter} done.` after that chapter commits.
The program writes `{book code} {chapter} failed.` when that chapter fails.
A failed run keeps every chapter that already committed.
The failed chapter is not stored.

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
The program asks again, up to 3 times, when a verse tag path fails the check.
A missing verse or an empty summary still stops the run.
Using Pre-Israelite, Israelite, or Non-Israelite in the wrong chapters is retried.
That request includes the original input, the response, and the check error.
The message list keeps the earlier questions and responses.
The program also writes `raw/{bookCode}-{chapter}-{attempt}.txt` for each of those bodies.
Any other check failure stops the run.
The program writes `verses.csv` and `chapters.jsonl` only after the check passes and the database transaction commits.

## Values to supply

No open values remain in this file.
