namespace BibleVerseExtractor.Services;

[BindScoped]
public class DbFillingService(FreeUseBibleService api, BibleMetadataService metadata, LabelingContext db)
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

        Console.WriteLine("Filling labels and book orders...");
        await FillMetadataAsync(entities);

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

    public async Task UpdateMetadataAsync()
    {
        Console.WriteLine("Clearing metadata...");
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.BookOrderItems.ExecuteDeleteAsync();
        await db.BookOrders.ExecuteDeleteAsync();
        await db.BookLabelMappings.ExecuteDeleteAsync();
        await db.BookLabels.ExecuteDeleteAsync();

        var books = await db.BibleBooks.ToListAsync();
        if (books.Count == 0)
            throw new InvalidOperationException("Database has no books. Fill the database before updating metadata.");

        Console.WriteLine("Filling labels and book orders...");
        await FillMetadataAsync(books);

        await transaction.CommitAsync();
        Console.WriteLine($"Updated metadata for {books.Count} books.");
    }

    async Task FillMetadataAsync(IReadOnlyList<BibleBook> books)
    {
        var booksByCode = books.ToDictionary(book => book.Code, StringComparer.Ordinal);

        var labels = Enum.GetValues<BibleBookLabel>().ToDictionary(
            label => label,
            label => new BookLabel { Name = BibleMetadataService.GetLabelName(label) });
        db.BookLabels.AddRange(labels.Values);

        foreach (var book in books)
        {
            foreach (var label in metadata.GetBook(book.Code).Labels)
            {
                db.BookLabelMappings.Add(new BookLabelMapping
                {
                    BookId = book.Id,
                    BookLabel = labels[label],
                });
            }
        }

        foreach (var order in metadata.Orders)
        {
            var entity = new BookOrder
            {
                Code = order.Code,
                Name = order.Name,
            };

            for (var index = 0; index < order.BookCodes.Length; index++)
            {
                var code = order.BookCodes[index];
                if (!booksByCode.TryGetValue(code, out var book))
                    throw new InvalidOperationException($"Book order '{order.Code}' includes '{code}', which was not loaded.");

                entity.Items.Add(new BookOrderItem
                {
                    BookId = book.Id,
                    Order = index + 1,
                });
            }

            db.BookOrders.Add(entity);
        }

        await db.SaveChangesAsync();
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
