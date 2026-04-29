using Bogus;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;

namespace LibraryManagement.IntegrationTests;

public static class SeedDataHelper
{
    private static readonly string[] Categories =
        { "Fiction", "Science", "History", "Technology", "Philosophy", "Art", "Medicine", "Law", "Education", "Sports" };

    public static async Task SeedAsync(LibraryDbContext context)
    {
        if (context.Books.Any())
            return;

        var bookFaker = new Faker<Book>()
            .RuleFor(b => b.Title, f => f.Lorem.Sentence(3))
            .RuleFor(b => b.Author, f => f.Name.FullName())
            .RuleFor(b => b.ISBN, f => f.Random.Replace("978-#-###-#####-#"))
            .RuleFor(b => b.TotalCopies, f => f.Random.Int(1, 10))
            .RuleFor(b => b.Category, f => f.PickRandom(Categories));

        var books = bookFaker.Generate(1000);
        foreach (var book in books)
            book.AvailableCopies = book.TotalCopies;

        context.Books.AddRange(books);
        await context.SaveChangesAsync();

        var memberFaker = new Faker<Member>()
            .RuleFor(m => m.FirstName, f => f.Name.FirstName())
            .RuleFor(m => m.LastName, f => f.Name.LastName())
            .RuleFor(m => m.Email, f => f.Internet.Email())
            .RuleFor(m => m.MembershipDate, f => DateTime.SpecifyKind(f.Date.Past(3), DateTimeKind.Utc))
            .RuleFor(m => m.IsActive, f => f.Random.Bool(0.9f));

        var members = memberFaker.Generate(2000);
        context.Members.AddRange(members);
        await context.SaveChangesAsync();

        var random = new Random(42);
        var loans = new List<Loan>();

        for (int i = 0; i < 7000; i++)
        {
            var book = books[random.Next(books.Count)];
            var member = members[random.Next(members.Count)];
            var borrowDate = DateTime.UtcNow.AddDays(-random.Next(1, 90));
            var dueDate = borrowDate.AddDays(14);
            var isReturned = random.NextDouble() > 0.3;
            var returnDate = isReturned ? borrowDate.AddDays(random.Next(1, 30)) : (DateTime?)null;

            decimal fine = 0;
            if (returnDate.HasValue && returnDate.Value > dueDate)
            {
                fine = (returnDate.Value.Date - dueDate.Date).Days * 0.50m;
            }

            loans.Add(new Loan
            {
                BookId = book.Id,
                MemberId = member.Id,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                ReturnDate = returnDate,
                Fine = fine
            });
        }

        context.Loans.AddRange(loans);
        await context.SaveChangesAsync();
    }
}
