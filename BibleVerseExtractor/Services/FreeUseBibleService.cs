using System.Net.Http.Json;
using System.Text.Json;

namespace BibleVerseExtractor.Services;

/// <summary>
/// API: https://bible.helloao.org/docs/guide/making-requests.html
/// </summary>
[BindSingleton]
public class FreeUseBibleService(ConfigProvider config)
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    readonly HttpClient client = new()
    {
        BaseAddress = new("https://bible.helloao.org/"),
    };

    CompleteTranslation? translation;

    public async Task<IReadOnlyList<BibleApiBook>> GetBooksAsync()
    {
        var payload = await GetTranslationAsync();

        return payload.Books
            .Where(book => book.IsApocryphal != true)
            .OrderBy(book => book.Order)
            .Select(book => new BibleApiBook(book.Id, book.CommonName, book.Order, book.TotalNumberOfVerses))
            .ToList();
    }

    public async Task<IReadOnlyList<BibleApiChapter>> GetChaptersAsync(string bookId)
    {
        var payload = await GetTranslationAsync();
        var book = payload.Books.FirstOrDefault(candidate => candidate.Id == bookId)
            ?? throw new InvalidOperationException($"The Bible API did not include book '{bookId}'.");

        return book.Chapters
            .Select(chapter =>
            {
                var number = chapter.Chapter.Number;
                var verses = chapter.Chapter.Content
                    .Where(content => content.Type == "verse")
                    .Select(content => new BibleApiVerse(
                        content.Number ?? throw new InvalidOperationException(
                            $"Book {bookId} chapter {number} has a verse without a number."),
                        content.Text ?? throw new InvalidOperationException(
                            $"Book {bookId} chapter {number} verse {content.Number} has no text.")))
                    .OrderBy(verse => verse.Number)
                    .ToList();

                return new BibleApiChapter(number, verses);
            })
            .OrderBy(chapter => chapter.Number)
            .ToList();
    }

    async Task<CompleteTranslation> GetTranslationAsync()
    {
        if (translation is not null)
            return translation;

        var url = $"api/{config.AppConfig.Translation}/complete.simple.json";
        translation = await client.GetFromJsonAsync<CompleteTranslation>(url, JsonOptions)
            ?? throw new InvalidOperationException($"The Bible API returned an empty response for '{url}'.");

        return translation;
    }

    sealed record CompleteTranslation(List<CompleteBook> Books);

    sealed record CompleteBook(
        string Id,
        string CommonName,
        int Order,
        int TotalNumberOfVerses,
        List<CompleteChapter> Chapters,
        bool? IsApocryphal = null);

    sealed record CompleteChapter(CompleteChapterBody Chapter);

    sealed record CompleteChapterBody(int Number, List<CompleteContent> Content);

    sealed record CompleteContent(string Type, int? Number, string? Text);
}

public sealed record BibleApiBook(string Id, string Name, int Order, int TotalNumberOfVerses);

public sealed record BibleApiChapter(int Number, IReadOnlyList<BibleApiVerse> Verses);

public sealed record BibleApiVerse(int Number, string Text);
