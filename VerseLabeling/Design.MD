# Verse labeling

This document defines the VerseLabeling program.
Each step has its own design file.
`Design-Step1.MD` defines step 1.

An example shows the shape of a value.
An example is not the approved catalog.
Do not copy an example row into the approved catalog.

## Terms

Use these names with the same meaning in every step.

- Step: one labeling task. The config field `Step` selects it.
- Request limit: the `Rpm` integer in the config. It is the maximum number of AI requests in 60 seconds.
- Verse tag: a named mark on a verse. One verse can have many verse tags.
- Parent tag: the verse tag above another verse tag. A verse tag has no parent, or one parent.
- SummarySoFar: the summary of this book through the chapter that wrote it.
- ImmediateContext: the topic text that the next chapter request needs.
- Role tag: Israelite, Non-Israelite, or Everyone.
- Group name: a specific name for one group. The model writes it. It is not from a fixed list.
- Run: one execution of one step. The table is `LabelRun`.
- Run result: the tags one run gave one verse. The table is `LabelRunTag`.
- Real tags: the tags a screen reads for a verse. The table is `VerseTagMapping`.
- Resolution: a human choice between two run results for one verse. The table is `VerseTagResolution`.

`BookLabel` marks a whole book.
A verse tag marks a verse.
Keep the two stores separate.

## Config

The program reads `project-conf.json`.
`ConfigProvider` loads that file into `AppConfig`.
Add `Ai` and `VerseLabeling` to `AppConfig`.
The program does not read process arguments.
A process argument is an error.
The program does not read `appsettings.json`.
A JSON null follows the same rule as a missing value.

`Ai` holds `BaseUrl` and `Model`.
`VerseLabeling` holds the run settings.
The API key is not in this file.
A missing `Ai` object is an error.
A missing `VerseLabeling` object is an error.
A missing `Step` is an error.
A blank `Step` is an error.
A step name with no design file is an error.

`VerseLabeling` fields:

- `Step`: the step name. Step 1 uses `label-is-israelites`.
- `Books`: the book codes, in run order.
- `Chapters`: the chapter range. An empty value selects every chapter.
- `Summary`: SummarySoFar for the first request of the run.
- `ImmediateContext`: ImmediateContext for the first request of the run.
- `Rpm`: the request limit.

A future step adds one step name and one design file.

Example of the two objects.
The book list, the range, and the two texts are examples.
`Rpm` 15 in the example is the default.
Empty `BaseUrl` and `Model` show the shape before the real values exist.
Those empty values are an error when the program starts.

```json
"Ai": {
  "BaseUrl": "",
  "Model": ""
},
"VerseLabeling": {
  "Step": "label-is-israelites",
  "Books": ["GEN", "EXO"],
  "Chapters": "5-",
  "Summary": "",
  "ImmediateContext": "",
  "Rpm": 15
}
```

## Errors

This rule applies to every error.
The program writes the error to the console.
The program exits with a non-zero code.
The program does not continue the run.
The program does not start that request again.

These cases are errors:

- The config is missing a required value.
- A config value is blank where this design forbids a blank value.
- A config value is invalid.
- The process has an argument.
- The API key is missing.
- The API key is blank.
- The API call fails.
- The API call returns no body.
- The body is not one JSON object.
- A JSON value has the wrong type.
- The step check fails.
- A database write fails.

The program validates the config before the first request.
The program does not insert a run row when the error is in that validation.
Every API response follows the next three rules.
The program writes a response body to the raw file before it checks the body.
The program does not write a raw file when the API returns no body.
The program writes that raw file once.
When the run row for this process exists, the program sets `Status` to `Failed` before it exits.
The program does not change a run row that another process inserted.
The program does not write the failed chapter to the success log.
The program does not write the failed chapter to the run result tables.
The program does not send the next chapter.
The program does not start another book.

The program exits with code 0 only after it sets `Status` to `Completed`.

## Books

`Books` is an array of book codes.
A book code is `BibleBook.Code`.
The program trims spaces around each code.
The program ignores letter case in each book code.
The program reads the books in the array order.
The log and the database store the code as `BibleBook.Code` stores it.
EXAMPLE: `["GEN", "EXO"]` selects Genesis, then Exodus.
The example shows the form. It is not a required book list.

These book values are errors:

- `Books` is missing.
- `Books` is empty.
- A code is blank.
- A value is not a book code.
- A book code repeats.
- A book has no verses.

The program writes the error.
The program exits.
The program does not send an AI request after that error.

## Chapters

`Chapters` accepts one continuous range.
Chapter numbers are 1-based.
The range includes both ends.
A single chapter uses the same start and end.
EXAMPLE: `1-1` selects chapter 1.
EXAMPLE: `1-2` selects chapter 1 and chapter 2.
EXAMPLE: `5-` selects chapter 5 through the last chapter of that book.
These examples show the form. They are not a required test range.

The program trims spaces at the ends of the value.
A space inside the value is an error.
The last chapter is the highest chapter number stored for that book.
The program checks every selected book before the first request.
A chapter that the book does not contain is an error.
A closed range whose end is lower than its start is an error.
A single number is an error.
EXAMPLE: `5` is an error.
A comma list of ranges is an error.
The program writes the error.
The program exits.

When `Chapters` is missing, the program selects every chapter of each selected book.
A blank value selects every chapter of each selected book.
In `run.txt` and in `LabelRun.Chapters`, that selection is the text `all`.

## First-request context

`Summary` sets SummarySoFar for the first request of the run.
`ImmediateContext` sets ImmediateContext for the first request of the run.
The first request may be chapter 1.
The first request may be a later chapter.

The program trims `Summary`.
The program trims `ImmediateContext`.
When `Summary` is missing, that field is the text `None`.
When `ImmediateContext` is missing, that field is the text `None`.
A blank value after the trim means the text `None`.
The value `None` means the text `None`.

The program uses the two values for the first request only.
Each later request in that same book uses the previous output in this run.
The first request of each later book uses the text `None` for SummarySoFar.
The first request of each later book uses the text `None` for ImmediateContext.
The program does not copy SummarySoFar from the previous book.
The program does not copy ImmediateContext from the previous book.

## Request limit

When `Rpm` is missing, the request limit is 15.
When `Rpm` is present, it accepts one positive integer.
That integer replaces the default.
Zero, a negative integer, or a value that is not an integer is an error.
The program writes the error.
The program exits.

Let N be the request limit.
The minimum gap is 60 divided by N seconds.
The program counts the gap from the start of the previous request.
The program waits for that response before the next request.
When the response arrives before the gap ends, the program waits for the rest of the gap.
The program does not start a new AI request during that gap.
One chapter uses one AI request.
The default gap is 4 seconds.

## AI client

The program uses one chat client for every step.
The client type is `IChatClient` from `Microsoft.Extensions.AI`.
Put that client in the VerseLabeling project.
Register one client for the process.
The client uses the OpenAI-compatible chat API.
WebClient references Common, so Common does not reference the AI packages.

Gemini and DeepSeek both offer an OpenAI-compatible chat API.
The same client calls one base URL and one model name.
This run uses the `BaseUrl` and `Model` in the config.
A person can point one run at the free Gemini API.
A person can point a later run at DeepSeek.
Change the config between those runs.
Each run stores the base URL and the model name that it used.

The API key is a Visual Studio user secret on the VerseLabeling project.
Add a `UserSecretsId` to `VerseLabeling.csproj`.
The secret name is `Ai:ApiKey`.
The program reads `Ai:ApiKey` from the VerseLabeling user secret store.
The secret file stays outside the git repository.
`project-conf.json` does not hold the API key.
A missing key is an error.
A blank key is an error.
A missing `BaseUrl` is an error.
A blank `BaseUrl` is an error.
A missing `Model` is an error.
A blank `Model` is an error.
The program writes the error.
The program exits.
The program does not write the API key to the log or the database.

Add an `Ai` object in `project-conf.json`.
`Ai` has `BaseUrl` and `Model`.
Keep both values empty in the repository until the real values exist.
The real URL and the real model name are values to supply.
The program still treats those empty values as errors.

Proposed Gemini base URL: `https://generativelanguage.googleapis.com/v1beta/openai/`
Proposed DeepSeek base URL: `https://api.deepseek.com`

The step asks the client for one JSON object.
The object must match the schema in that step's design file.
The step checks the object.
The step writes a success log only after the check succeeds.
The step writes the database rows for that chapter only after the check succeeds.

Add `Microsoft.Extensions.AI` to VerseLabeling.
Add `Microsoft.Extensions.AI.OpenAI` to VerseLabeling.
Add `Microsoft.Extensions.Configuration.UserSecrets` to VerseLabeling.

## Database

Add these entities in the Common project, with the other entities.
Add a `DbSet` for each one on `LabelingContext`.

- `VerseTag`
- `VerseTagMapping`
- `LabelRun`
- `LabelRunTag`
- `VerseTagResolution`

Only the user creates the migration.
Only the user applies the migration.

The labeling program opens the database from `project-conf.json`.
The program does not call `InitializeAsync`.
The program does not migrate the database.

### VerseTag

Columns:

- `Id`: integer key.
- `Name`: required text. The name is unique.
- `ParentId`: null, or the `Id` of the parent tag.

The unique comparison on `Name` is ordinal and case-sensitive.
A verse tag has no parent, or one parent.
The parent is another verse tag.
A child tag can also be a parent.
`ParentId` must not be the `Id` of the same tag.
`ParentId` must not be the `Id` of a descendant of that tag.

EXAMPLE: Non-Israelite is the parent of Amorite and of Jebusite.
Those names are examples. They are not the catalog.

The database rejects a delete of a tag that still has a child.
The database rejects a delete of a tag that still has a verse mapping.
The database rejects a delete of a tag that still has a run result.

Index `ParentId` on `VerseTag`.

### VerseTagMapping

These rows are the real tags.
A screen reads this table.
A screen does not build the shown tags from the run tables.

Columns:

- `Id`: integer key.
- `VerseId`: required foreign key to `BibleVerse`.
- `VerseTagId`: required foreign key to `VerseTag`.
- `TagIndex`: required integer. It starts at 1.

The unique key column order is `VerseId`, then `VerseTagId`.
The pair `VerseId`, `TagIndex` is unique.
One verse can have many verse tags.
One verse tag can mark many verses.

Index `VerseTagId` on `VerseTagMapping`.

### LabelRun

One row is one run.
The program inserts the row after config validation and before the first request.
`Status` starts as `Running`.
The program sets `Status` to `Completed` after the last selected chapter is stored.
The program sets `Status` to `Failed` when an error occurs after this insert.

Columns:

- `Id`: integer key.
- `RunKey`: required text. Unique. The UTC start time, compact, as in `20261026T160800Z`. The same value is the log folder name.
- `Step`: required text.
- `Books`: required text. Canonical book codes in list order, separated by a comma.
- `Chapters`: required text. The config range, or the text `all`.
- `Summary`: required text. The first-request SummarySoFar, or the text `None`.
- `ImmediateContext`: required text. The first-request ImmediateContext, or the text `None`.
- `Rpm`: required integer.
- `BaseUrl`: required text.
- `Model`: required text.
- `Status`: required text. The values are `Running`, `Completed`, and `Failed`.

The database rejects a delete of a run that still has a run result.
The database rejects a delete of a run that a resolution names.
A second run with the same `RunKey` is an error.
That insert fails.
The program does not send a request for the failed insert.
The program does not change the row that already has that `RunKey`.

### LabelRunTag

One row is one tag from one run on one verse.
The program writes these rows only after the chapter check succeeds.
One transaction holds the new `VerseTag` rows, the `LabelRunTag` rows, and the real-tag update for that chapter.
The program writes every selected verse of that chapter in that transaction.
`TagIndex` follows the order of the `Tags` array from the model.
`TagIndex` starts at 1.

Columns:

- `Id`: integer key.
- `LabelRunId`: required foreign key to `LabelRun`.
- `VerseId`: required foreign key to `BibleVerse`.
- `VerseTagId`: required foreign key to `VerseTag`.
- `TagIndex`: required integer.

The unique key column order is `LabelRunId`, then `VerseId`, then `TagIndex`.
The unique key column order for the tag is `LabelRunId`, then `VerseId`, then `VerseTagId`.
Index `VerseId` on `LabelRunTag`.
Index `VerseTagId` on `LabelRunTag`.

The program creates a `VerseTag` when that exact name is absent.
The program reuses the `VerseTag` when that exact name is present.
The program sets `ParentId` to null on a tag that it creates.
The program does not change `ParentId` on a tag that already exists.
The program does not merge names.
EXAMPLE: Egypt and Egyptian stay as two tags.
That pair is an example. It is not a required merge.

### Real tags during a run

The latest run for a verse is the run with the greatest `RunKey` among runs that have a `LabelRunTag` for that verse.
`RunKey` sorts in time order.

That chapter transaction updates the real tags for each verse in that chapter.

- When a `VerseTagResolution` exists for the verse, the program does not change `VerseTagMapping`.
- When no resolution exists, the program replaces `VerseTagMapping` for that verse with the tags from this run.
- The replacement keeps the `TagIndex` order from this run.

A later run still stores its own `LabelRunTag` rows when a resolution exists.
That later run does not change the resolution.
That later run does not change the real tags.

### VerseTagResolution

A human saves this row through a later program.
This design does not define that program.
The labeling program does not write this table.

The row says that a human resolved the verse.
The decision is run A or run B.
The chosen run supplies the real tags.

Columns:

- `Id`: integer key.
- `VerseId`: required foreign key to `BibleVerse`. Unique.
- `RunAId`: required foreign key to `LabelRun`.
- `RunBId`: required foreign key to `LabelRun`.
- `ChosenRunId`: required foreign key to `LabelRun`.

`RunAId` and `RunBId` are different runs.
`ChosenRunId` is `RunAId` or `RunBId`.
The database rejects any other combination.
The save rejects a run that has no `LabelRunTag` for that verse.
The save copies that chosen run's tags into `VerseTagMapping` for that verse.
The copy keeps the chosen run's `TagIndex` order.
The resolution row does not copy the tag names.
A later edit to `LabelRunTag` does not change `VerseTagMapping`.

### Conflicts

A verse is in conflict when all of these are true:

- No `VerseTagResolution` exists for the verse.
- Two or more runs have `LabelRunTag` rows for the verse.
- Those runs do not all use the same set of `VerseTagId` values.

`TagIndex` is not part of that comparison.
The same tag ids in a different order are not a conflict.
Before a human saves a resolution, the real tags stay on the latest run.
That remains true while the verse is in conflict.
The labeling program does not insert a conflict row.
A later program finds conflicts by this rule.

EXAMPLE: run 1 tags a verse Israelite.
Run 2 tags that verse Israelite and Egypt.
The verse is in conflict.
The real tags show run 2 until a human chooses run 1 or run 2.
Those names are examples. They are not a required verse.

## Log folder

The program writes each run under `out/verse-labeling/{step}/{run-id}/`.
Git ignores the `out` folder.
`{run-id}` is `LabelRun.RunKey`.
EXAMPLE: `20261026T160800Z`.
That time is an example of the id shape.

The files are the raw log.
Each step design defines the files inside the run folder.
The same run also stores its result in the database.
The raw log stays in these files.

## Values to supply

Do not start a full run until these values exist.

1. The Gemini base URL and model name for the free tier.
2. The DeepSeek base URL and model name, before any paid run.
