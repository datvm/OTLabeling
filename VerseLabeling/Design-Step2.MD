# Step 2: topic labels

This file defines the step `label-topics`.
Read `Design.MD` for the config, the error rule, the request limit, the database, and the log folder.
This step reads `BibleBook`, `BibleVerse`, `ChapterContext`, and `Topic`.
This step writes `Topic`, `LabelRun`, `LabelRunChapter`, `LabelRunTopic`, `LabelRunTopicAnswer`, and `VerseTopicAnswer`.
This step does not write `ChapterContext`.
This step does not write `VerseTag`, `VerseTagParent`, `VerseTagMapping`, or `VerseTagResolution`.
This step writes a log.

The program does not read this file to build the request.

## What this step asks

The program sends one chapter in each Jev request.
The state holds the rules, the chapter, and the previous chapter context when that context exists.
The questions are the topics.
One topic is one yes/no question for each verse in the chapter.
Jev returns the probability that the answer is yes.
The program stores Yes, No, or Ambiguous from that probability.
The program stores that probability with the answer.

A probability of 0.7 or higher is Yes.
A probability of 0.3 or lower is No.
A probability between those values is Ambiguous.
The stored probability is the probability that the answer is yes.

## Topics

`Topics` is an array in `VerseLabeling`.
The array order is the topic order.
Each topic has `Code`, `Title`, and `Question`.
The program trims each value.
A missing `Topics` array is an error on a new run.
An empty array is an error on a new run.
A blank code is an error.
A code with a space, a comma, or a line break is an error.
A repeated code is an error.
The comparison ignores capital letters.
`label-is-israelites` does not read `Topics`.

The program stores one `Topic` row for each code.
A new run updates `Topic` from `Topics` before it sends a request.
A code that is not in `Topic` is inserted.
An insert needs a title and a question.
A title with a letter or a digit replaces the stored title.
A question with a letter or a digit replaces the stored question.
A missing title, a blank title, or a title with no letter and no digit keeps the stored title.
A missing question, a blank question, or a question with no letter and no digit keeps the stored question.
The program then copies the stored code, title, and question onto `LabelRunTopic`.
A resumed run does not read `Topics`.
A resumed run does not change `Topic`.
A resumed run uses the `LabelRunTopic` rows stored on that run.

The code and the title do not go to Jev.
The question goes to Jev.
The question id is for the program.
The id is the verse number and the topic order.
The id does not contain the code.

## Previous chapter context

Chapter 1 has no previous chapter.
The program reads `ChapterContext` for the previous chapter number in the same book.
The program sends that row when both texts contain a letter or a digit.
The program omits the previous context when that row is absent.
The program omits the previous context when either text has no letter and no digit.
The program does not copy context from the previous book.
The program does not send the `Summary` or `ImmediateContext` config values to Jev.
The program does not write `ChapterContext`.

## Request

The program sends the request to `https://api.typesafe.ai/v1/systemone`.
The model is `Jev.Model`.
A missing `Jev` object is an error.
A missing model is an error.
A blank model is an error.
The API key is the VerseLabeling user secret `Jev:ApiKey`.
A missing key is an error.
A blank key is an error.
The program does not write the key to the log or the database.
The program does not read `Ai` or `Ai:ApiKey` for this step.

The state is one JSON object.
`rules` tells Jev to use only `chapter`, and `previous` when the request includes it.
`previous.summarySoFar` is the book summary through the previous chapter.
`previous.immediateContext` is the topic of that chapter.
`chapter` is the verses of this chapter, in verse order.
Each verse has `verseNumber` and `text`.
A line break in the verse text becomes a space.
`previous` is absent when this chapter has no stored context.

Each question is a yes/no question.
The question text names the verse in `chapter`, then gives the topic question.
The named verse must state the fact the question asks about.
Another verse may identify who or what a word in the named verse refers to.
That includes a pronoun.
Another verse does not supply an action, a speech, a promise, a judgment, or an attitude.
A Yes for one question does not make another question Yes.
A nation, people, or group counts only when `chapter` or `previous` names it.
A promise or a blessing is a word from God, or a report of that word.
A human offer, agreement, marriage proposal, or wish is not that promise.
Hostility, a threat, conflict, or judgment counts only when the named verse states it.
An Israelite is a person or group in the line of promise that begins with Abraham in Genesis 12 and continues through Isaac, Jacob, and their descendants.
The program uses Israelite only from Genesis 12 onward.
Before Genesis 12, a person or group is not Israelite and not a non-Israelite.
Hebrew, children of Israel, house of Jacob, and a similar expression count as Israelite when `chapter` or `previous` makes that identification.
A non-Israelite is a person or group outside that line.
A foreigner is a non-Israelite.
A sojourner counts as a foreigner when `chapter` or `previous` places that person outside the Israelite line.

The program stores `https://api.typesafe.ai` in `LabelRun.BaseUrl`.
The program stores `Jev.Model` in `LabelRun.Model`.
`LabelRun.Summary` and `LabelRun.ImmediateContext` store the config values.
Those two config values are not sent to Jev.

Example of the request shape.
The verse and the question are examples.

```json
{
  "model": "jev-latest",
  "state": {
    "rules": "{rules}",
    "previous": {
      "summarySoFar": "{text}",
      "immediateContext": "{text}"
    },
    "chapter": [
      { "verseNumber": 1, "text": "{verse text}" }
    ]
  },
  "questions": {
    "v1t1": {
      "type": "noul",
      "instructions": "For verse 1 in `chapter`: {question}"
    }
  }
}
```

## Checks

The program checks the response before the success log and before the database write.
`Design.MD` defines the error rule.
The response is one JSON object.
`answers` has one yes/no value for each question sent in that request.
A missing answer fails the check.
An extra answer fails the check.
A value that is not a yes/no answer fails the check.
A probability below 0 or above 1 fails the check.
The program does not ask Jev to repair the response.
The program writes the raw response before the check.
The program does not write a raw file when the response has no body.
The program does not store the chapter when the check fails.
A failed check leaves that chapter's `LabelRunChapter.Filled` false.

The default context length is 64,000 tokens for the state and all questions together.
The state and one question together must also fit in 32,000 tokens.
HTTP 400 with `max_tokens_exceeded` means the request is over that length.
The program sends the same request again with the header `TypeSafe-Danger-Zone-Long-Context` set to `true`.
That header raises both limits to 262,000 tokens.
The program sends that header only after `max_tokens_exceeded`.
A second `max_tokens_exceeded` stops the run.

HTTP 429, HTTP 503, and HTTP 529 use the retry rule in `Design.MD`.
HTTP 529 is the overloaded response from Jev.
Any other API failure stops the run.

## Stored rows

The program inserts `LabelRun` and one `LabelRunChapter` for every selected chapter before the first request.
The same transaction inserts a missing `Topic` and updates a stored title or question when the config supplies one.
The same transaction inserts one `LabelRunTopic` for every topic.
`LabelRunTopic` stores the code, the title, the question, and the order copied from `Topic`.
A later run does not change those `LabelRunTopic` rows.

`Topic` columns:

- `Id`: integer key.
- `Code`: required text. The unique key.
- `Title`: required text.
- `Question`: required text.

The code comparison ignores capital letters.
`clear-runs` does not delete `Topic`.

`LabelRunTopic` columns:

- `Id`: integer key.
- `LabelRunId`: required foreign key to `LabelRun`.
- `SortOrder`: required integer. It starts at 1 and follows the `Topics` array.
- `Code`: required text.
- `Title`: required text.
- `Question`: required text.

The unique key is `LabelRunId`, then `SortOrder`.
The unique key is `LabelRunId`, then `Code`.
The code comparison ignores capital letters.

`LabelRunTopicAnswer` is one answer from one run for one verse and one topic.
The program writes these rows after the check succeeds.
One transaction writes every verse of that chapter.

`LabelRunTopicAnswer` columns:

- `Id`: integer key.
- `LabelRunId`: required foreign key to `LabelRun`.
- `VerseId`: required foreign key to `BibleVerse`.
- `LabelRunTopicId`: required foreign key to `LabelRunTopic`.
- `Answer`: required text. The values are `Yes`, `No`, and `Ambiguous`.
- `Probability`: required number from 0 to 1. It is the probability that the answer is yes.

The unique key is `LabelRunId`, then `VerseId`, then `LabelRunTopicId`.

`VerseTopicAnswer` is the current answer for one verse and one topic code.
The same transaction replaces the current row for each code in this run.
A code that this run does not ask stays as it is.
The current row records the run that wrote it.

`VerseTopicAnswer` columns:

- `Id`: integer key.
- `VerseId`: required foreign key to `BibleVerse`.
- `Code`: required text. The topic code.
- `Answer`: required text. The values are `Yes`, `No`, and `Ambiguous`.
- `Probability`: required number from 0 to 1.
- `LabelRunId`: required foreign key to `LabelRun`. The run that wrote this row.

The unique key is `VerseId`, then `Code`.
The code comparison ignores capital letters.

The database rejects a delete of a run that still has a topic.
The database rejects a delete of a run that still has a topic answer.
The database rejects a delete of a topic that still has a topic answer.
The database rejects a delete of a verse that still has a topic answer.

## Chapter order

`Design.MD` defines `ResumeRun` and `LabelRunChapter`.
A new run inserts one `LabelRunChapter` for every selected chapter before the first request.
The program reads the books in the `Books` array order.
The program completes one book before the next book.
Inside a book, the program sends the selected chapters in ascending order.
When the check succeeds, the program writes that chapter to the database.
The program appends that chapter to the log after the database transaction commits.
The program sends the next request after that append.
The program writes `{book code} {chapter} started.` before that request.
The program writes the token line after each response that has a body.
Jev does not return a cost.
That line says the cost is not available.
The program writes `{book code} {chapter} done.` after that chapter commits.
The program writes `{book code} {chapter} failed.` when that chapter fails.
A failed run keeps every chapter that already committed.
The failed chapter is not stored.

A resumed run sends only the chapters whose `Filled` is false.
The program uses the current `Rpm`, `RetryDelaySeconds`, and `Jev.Model`.
The program writes those values back onto the run.
The program writes `Run {id} resumed.` and the count of remaining chapters.
When no chapter is missing, the program sets `Status` to `Completed`.
The program writes that the run has no missing chapters.
The program does not send a request.

## Log files

The run folder is `out/verse-labeling/label-topics/{run-id}/`.
`Design.MD` defines `{run-id}`.

The program writes these files:

- `run.txt` for the run settings.
- `topics.json` for the code, the title, and the question.
- `verses.csv` for one row per verse and topic.
- `raw/{bookCode}-{chapter}.txt` for the response body.

`{bookCode}` is `BibleBook.Code`.

The program writes `run.txt` when it starts the requests for the run.
`run.txt` lists these fields:

- the step
- the book selection
- the chapter selection
- the topic codes, in topic order
- the request limit
- the retry delay
- the base URL
- the model name
- the UTC start

`run.txt` does not contain the API key.
`run.txt` does not contain the question text.

Example of `run.txt`. The books, the range, the topic, and the time are examples.

```text
step=label-topics
books=GEN,EXO
chapters=1-1
topics=HasForeigner
rpm=15
retryDelaySeconds=30
baseUrl=https://api.typesafe.ai
model=jev-latest
startedUtc=20261107T033400Z
```

`topics.json` is one JSON array.
Each object has `Code`, `Title`, and `Question`.
The array order is the topic order.
The program writes this file when it writes `run.txt`.

`verses.csv` uses UTF-8.
The file has a header row.
Columns: `BookCode`, `Chapter`, `VerseNumber`, `Code`, `Answer`, `Probability`.
One verse with two topics has two rows.
Row order is chapter order, then verse number, then topic order.
The program writes `verses.csv` only after the check passes and the database transaction commits.

The program writes `raw/{bookCode}-{chapter}.txt` for every response body.
The program writes that file before the check.
A failed check does not write that chapter to `verses.csv`.
A failed check does not write that chapter to `LabelRunTopicAnswer` or `VerseTopicAnswer`.

## Values to supply

The user secret `Jev:ApiKey` on the VerseLabeling project.
The user adds and applies the migration for `Topic`, `LabelRunTopic`, `LabelRunTopicAnswer`, and `VerseTopicAnswer`.
