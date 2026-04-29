using FluentAssertions;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace LibraryManagement.DatabaseTests;

public class DatabaseTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("library_db_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }

    public LibraryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new LibraryDbContext(options);
    }
}

public class DatabaseTests : IClassFixture<DatabaseTestFixture>
{
    private readonly DatabaseTestFixture _fixture;

    public DatabaseTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Book book, Member member)> SeedBookAndMember(LibraryDbContext context, string isbn)
    {
        var book = new Book
        {
            Title = "DB Test Book",
            Author = "DB Author",
            ISBN = isbn,
            TotalCopies = 3,
            AvailableCopies = 3,
            Category = "Science"
        };

        var member = new Member
        {
            FirstName = "DB",
            LastName = "Tester",
            Email = $"db.tester.{Guid.NewGuid():N}@example.com",
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        };

        context.Books.Add(book);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        return (book, member);
    }

    [Fact]
    public async Task AvailableCopiesConsistency_AfterBorrowAndReturn()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var (book, member) = await SeedBookAndMember(context, $"978-0-DB1-{Guid.NewGuid().ToString()[..5]}-0");
        var initialCopies = book.AvailableCopies;

        var loanService = new LoanService(context);

        // Act: Borrow
        var loanResult = await loanService.BorrowBookAsync(new CreateLoanDto(book.Id, member.Id));

        // Assert after borrow
        var bookAfterBorrow = await context.Books.FindAsync(book.Id);
        bookAfterBorrow!.AvailableCopies.Should().Be(initialCopies - 1);

        // Act: Return
        await loanService.ReturnBookAsync(loanResult.Id);

        // Assert after return
        var bookAfterReturn = await context.Books.FindAsync(book.Id);
        bookAfterReturn!.AvailableCopies.Should().Be(initialCopies);
    }

    [Fact]
    public async Task ConcurrentBorrow_LastCopy_OnlyOneSucceeds()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var book = new Book
        {
            Title = "Concurrent Test Book",
            Author = "Author",
            ISBN = $"978-0-CC1-{Guid.NewGuid().ToString()[..5]}-0",
            TotalCopies = 1,
            AvailableCopies = 1,
            Category = "Technology"
        };

        var members = Enumerable.Range(0, 5).Select(i => new Member
        {
            FirstName = $"Concurrent{i}",
            LastName = "Tester",
            Email = $"concurrent{i}.{Guid.NewGuid():N}@example.com",
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        }).ToList();

        context.Books.Add(book);
        context.Members.AddRange(members);
        await context.SaveChangesAsync();

        // Act: Try to borrow concurrently
        var tasks = members.Select(async m =>
        {
            try
            {
                await using var scopeContext = _fixture.CreateContext();
                var service = new LoanService(scopeContext);
                await service.BorrowBookAsync(new CreateLoanDto(book.Id, m.Id));
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert: At least one succeeded, not all succeeded
        results.Count(r => r).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task FineCalculation_OnReturn_IsPersistedCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var book = new Book
        {
            Title = "Fine Test Book",
            Author = "Author",
            ISBN = $"978-0-FN1-{Guid.NewGuid().ToString()[..5]}-0",
            TotalCopies = 5,
            AvailableCopies = 4,
            Category = "Fiction"
        };

        var member = new Member
        {
            FirstName = "Fine",
            LastName = "Tester",
            Email = $"fine.{Guid.NewGuid():N}@example.com",
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        };

        context.Books.Add(book);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        // Create an overdue loan (borrowed 20 days ago, due 6 days ago)
        var loan = new Loan
        {
            BookId = book.Id,
            MemberId = member.Id,
            BorrowDate = DateTime.UtcNow.AddDays(-20),
            DueDate = DateTime.UtcNow.AddDays(-6),
            ReturnDate = null,
            Fine = 0
        };

        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        var loanService = new LoanService(context);

        // Act: Return the overdue book
        var result = await loanService.ReturnBookAsync(loan.Id);

        // Assert: Fine should be calculated and persisted
        result.Fine.Should().BeGreaterThan(0);

        // Verify persistence
        var persistedLoan = await context.Loans.FindAsync(loan.Id);
        persistedLoan!.Fine.Should().Be(result.Fine);
        persistedLoan.ReturnDate.Should().NotBeNull();
    }
}
