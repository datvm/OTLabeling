# Clear runs

This file defines the step `clear-runs`.
Read `Design.MD` for the config and the error rule.
The program does not read this file to decide what to delete.

Set `VerseLabeling.Step` to `clear-runs`.
The program deletes the labeling rows.
The program deletes the run logs.
The program keeps the extracted Bible and its metadata.
The program does not call the API.
The program does not read the API key.
The program does not read `Books`, `Chapters`, `Summary`, `ImmediateContext`, `Rpm`, or `RetryDelaySeconds` for this step.
The program does not insert a `LabelRun` row.

The program deletes these tables when they exist, in this order:

- `VerseTagResolutions`
- `LabelRunTags`
- `VerseTagMappings`
- `VerseTagParents`
- `VerseTags`
- `LabelRuns`

One transaction holds those deletes.
The program keeps `BibleBook`, `BibleVerse`, `BookLabel`, `BookLabelMapping`, `BookOrder`, and `BookOrderItem`.

The program deletes the folder `out/verse-labeling`.
That folder holds the run logs.
The program writes how many label runs and verse tags it cleared.
The program writes that it cleared the run logs.

Set `Step` back to `label-is-israelites` before the next labeling run.
