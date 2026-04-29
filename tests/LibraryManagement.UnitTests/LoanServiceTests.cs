using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryManagement.UnitTests;

public class LoanServiceTests : IDisposable
{
    private readonly LibraryDbContext _context;
    private readonly LoanService _sut;
    private readonly IFixture _fixture;

    public LoanServiceTests()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LibraryDbContext(options);
        _sut = new LoanService(_context);

        _fixture = new Fixture();
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // --- CalculateFine Tests ---

    [Fact]
    public void CalculateFine_WhenReturnedOnTime_ReturnsZero()
    {
        // Arrange
        var dueDate = DateTime.UtcNow;
        var returnDate = dueDate.AddDays(-1);

        // Act
        var fine = _sut.CalculateFine(dueDate, returnDate);

        // Assert
        fine.Should().Be(0m);
    }

    [Fact]
    public void CalculateFine_WhenReturnedOnDueDate_ReturnsZero()
    {
        // Arrange
        var dueDate = new DateTime(2024, 6, 15);
        var returnDate = new DateTime(2024, 6, 15);

        // Act
        var fine = _sut.CalculateFine(dueDate, returnDate);

        // Assert
        fine.Should().Be(0m);
    }

    [Fact]
    public void CalculateFine_WhenReturned1DayLate_Returns0_50()
    {
        // Arrange
        var dueDate = new DateTime(2024, 6, 15);
        var returnDate = new DateTime(2024, 6, 16);

        // Act
        var fine = _sut.CalculateFine(dueDate, returnDate);

        // Assert
        fine.Should().Be(0.50m);
    }

    [Fact]
    public void CalculateFine_WhenReturned5DaysLate_Returns2_50()
    {
        // Arrange
        var dueDate = new DateTime(2024, 6, 15);
        var returnDate = new DateTime(2024, 6, 20);

        // Act
        var fine = _sut.CalculateFine(dueDate, returnDate);

        // Assert
        fine.Should().Be(2.50m);
    }

    [Fact]
    public void CalculateFine_WhenReturned30DaysLate_Returns15()
    {
        // Arrange
        var dueDate = new DateTime(2024, 6, 15);
        var returnDate = new DateTime(2024, 7, 15);

        // Act
        var fine = _sut.CalculateFine(dueDate, returnDate);

        // Assert
        fine.Should().Be(15.00m);
    }

    // --- BorrowBook Tests ---

    [Fact]
    public async Task BorrowBook_WhenNoAvailableCopies_ThrowsException()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, "978-0-111-11111-0")
            .With(b => b.TotalCopies, 1)
            .With(b => b.AvailableCopies, 0)
            .Without(b => b.Loans)
            .Create();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, true)
            .Without(m => m.Loans)
            .Create();

        _context.Books.Add(book);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var dto = new CreateLoanDto(book.Id, member.Id);

        // Act
        var act = () => _sut.BorrowBookAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*available*");
    }

    [Fact]
    public async Task BorrowBook_WhenMemberIsInactive_ThrowsException()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, "978-0-222-22222-0")
            .With(b => b.TotalCopies, 5)
            .With(b => b.AvailableCopies, 5)
            .Without(b => b.Loans)
            .Create();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, false) // Inactive!
            .Without(m => m.Loans)
            .Create();

        _context.Books.Add(book);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var dto = new CreateLoanDto(book.Id, member.Id);

        // Act
        var act = () => _sut.BorrowBookAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Inactive*");
    }

    [Fact]
    public async Task BorrowBook_WhenMemberHas5ActiveLoans_ThrowsException()
    {
        // Arrange
        var books = Enumerable.Range(0, 6).Select(i => _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, $"978-0-333-3333{i}-0")
            .With(b => b.TotalCopies, 5)
            .With(b => b.AvailableCopies, 5)
            .Without(b => b.Loans)
            .Create()).ToList();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, true)
            .Without(m => m.Loans)
            .Create();

        _context.Books.AddRange(books);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        // Create 5 active loans
        for (int i = 0; i < 5; i++)
        {
            var loan = new Loan
            {
                BookId = books[i].Id,
                MemberId = member.Id,
                BorrowDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                ReturnDate = null, // Active loan
                Fine = 0
            };
            _context.Loans.Add(loan);
        }
        await _context.SaveChangesAsync();

        var dto = new CreateLoanDto(books[5].Id, member.Id);

        // Act
        var act = () => _sut.BorrowBookAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum*");
    }

    [Fact]
    public async Task BorrowBook_WhenMemberHas4ActiveLoans_Succeeds()
    {
        // Arrange
        var books = Enumerable.Range(0, 5).Select(i => _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, $"978-0-444-4444{i}-0")
            .With(b => b.TotalCopies, 5)
            .With(b => b.AvailableCopies, 5)
            .Without(b => b.Loans)
            .Create()).ToList();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, true)
            .Without(m => m.Loans)
            .Create();

        _context.Books.AddRange(books);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        // Create 4 active loans
        for (int i = 0; i < 4; i++)
        {
            var loan = new Loan
            {
                BookId = books[i].Id,
                MemberId = member.Id,
                BorrowDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                ReturnDate = null,
                Fine = 0
            };
            _context.Loans.Add(loan);
        }
        await _context.SaveChangesAsync();

        var dto = new CreateLoanDto(books[4].Id, member.Id);

        // Act
        var result = await _sut.BorrowBookAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.BookId.Should().Be(books[4].Id);
    }

    [Fact]
    public async Task BorrowBook_SetsDueDateTo14DaysFromBorrowDate()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, "978-0-555-55555-0")
            .With(b => b.TotalCopies, 3)
            .With(b => b.AvailableCopies, 3)
            .Without(b => b.Loans)
            .Create();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, true)
            .Without(m => m.Loans)
            .Create();

        _context.Books.Add(book);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var dto = new CreateLoanDto(book.Id, member.Id);

        // Act
        var result = await _sut.BorrowBookAsync(dto);

        // Assert
        var expectedDue = result.BorrowDate.AddDays(14);
        result.DueDate.Should().BeCloseTo(expectedDue, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task BorrowBook_DecreasesAvailableCopies()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, "978-0-666-66666-0")
            .With(b => b.TotalCopies, 3)
            .With(b => b.AvailableCopies, 3)
            .Without(b => b.Loans)
            .Create();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, true)
            .Without(m => m.Loans)
            .Create();

        _context.Books.Add(book);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var dto = new CreateLoanDto(book.Id, member.Id);

        // Act
        await _sut.BorrowBookAsync(dto);

        // Assert
        var updatedBook = await _context.Books.FindAsync(book.Id);
        updatedBook!.AvailableCopies.Should().Be(2);
    }

    // --- ReturnBook Tests ---

    [Fact]
    public async Task ReturnBook_UpdatesAvailableCopies()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, "978-0-777-77777-0")
            .With(b => b.TotalCopies, 3)
            .With(b => b.AvailableCopies, 2)
            .Without(b => b.Loans)
            .Create();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, true)
            .Without(m => m.Loans)
            .Create();

        _context.Books.Add(book);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var loan = new Loan
        {
            BookId = book.Id,
            MemberId = member.Id,
            BorrowDate = DateTime.UtcNow.AddDays(-7),
            DueDate = DateTime.UtcNow.AddDays(7),
            ReturnDate = null,
            Fine = 0
        };
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Act
        await _sut.ReturnBookAsync(loan.Id);

        // Assert
        var updatedBook = await _context.Books.FindAsync(book.Id);
        updatedBook!.AvailableCopies.Should().Be(3);
    }

    [Fact]
    public async Task ReturnBook_WhenAlreadyReturned_ThrowsException()
    {
        // Arrange
        var book = _fixture.Build<Book>()
            .With(b => b.Id, 0)
            .With(b => b.ISBN, "978-0-888-88888-0")
            .With(b => b.TotalCopies, 3)
            .With(b => b.AvailableCopies, 3)
            .Without(b => b.Loans)
            .Create();

        var member = _fixture.Build<Member>()
            .With(m => m.Id, 0)
            .With(m => m.IsActive, true)
            .Without(m => m.Loans)
            .Create();

        _context.Books.Add(book);
        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        var loan = new Loan
        {
            BookId = book.Id,
            MemberId = member.Id,
            BorrowDate = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(4),
            ReturnDate = DateTime.UtcNow, // Already returned
            Fine = 0
        };
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _sut.ReturnBookAsync(loan.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been returned*");
    }
}
