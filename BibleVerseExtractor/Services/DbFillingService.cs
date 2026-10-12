namespace BibleVerseExtractor.Services;

[BindScoped]
public class DbFillingService(FreeUseBibleService api, LabelingContext db)
{

    public async Task FillDbAsync()
    {
        Console.WriteLine("Validating database...");
        await ValidateAsync();

        Console.WriteLine("Fetching books...");
        var books = await api.GetBooksAsync();

        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        await using var transaction = await db.Database.BeginTransactionAsync();

        var entities = books.Select(book => new BibleBook
        {
            Code = book.Id,
            Name = book.Name,
        }).ToList();
        db.BibleBooks.AddRange(entities);
        await db.SaveChangesAsync();

        for (var index = 0; index < books.Count; index++)
        {
            var book = books[index];
            var bookId = entities[index].Id;
            var accumulated = 0;

            Console.WriteLine($"Filling {book.Name}...");
            foreach (var chapter in await api.GetChaptersAsync(book.Id))
            {
                foreach (var verse in chapter.Verses)
                {
                    accumulated++;
                    db.BibleVerses.Add(new BibleVerse
                    {
                        BookId = bookId,
                        Chapter = chapter.Number,
                        VerseNumber = verse.Number,
                        Text = verse.Text,
                        AccumulatedVerseNumber = accumulated,
                    });
                }
            }

            if (accumulated != book.TotalNumberOfVerses)
            {
                throw new InvalidOperationException(
                    $"{book.Name} should contain {book.TotalNumberOfVerses} verses, but the API returned {accumulated}.");
            }

            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }

        await transaction.CommitAsync();
        Console.WriteLine($"Filled {books.Count} books.");
    }

    public async Task ClearAsync()
    {
        await db.BibleBooks.ExecuteDeleteAsync();
    }

    async Task ValidateAsync()
    {
        if (await db.BibleBooks.AnyAsync())
        {
            throw new InvalidOperationException("Database is not empty. Please clear the database before filling it.");
        }
    }

}
