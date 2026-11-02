# Execution

Do not start the application.
Do not add or apply apply database migrations.
The user starts the application.
The user applies the migrations.

# Razor

Wrap every code value in `@()`.
This applies to an attribute value.
This applies to element content.
This applies to a string value.
This applies to a value that is not a string.
Write `@(verse.Text)`.
Write `@(verse.VerseNumber)`.
Do not write the value without `@()`.
Do not write `@verse.Text`.
Leave `@if`, `@foreach`, `@code`, and the other Razor directives as they are.