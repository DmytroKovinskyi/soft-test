using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibraryManagement.IntegrationTests;

public class LoanIntegrationTests : IClassFixture<LibraryApiFactory>
{
    private readonly HttpClient _client;
    private readonly LibraryApiFactory _factory;

    public LoanIntegrationTests(LibraryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BorrowAndReturn_FullFlow_ReturnsCorrectState()
    {
        // Arrange: Create a book and member for this test
        var bookDto = new CreateBookDto("Full Flow Test Book", "Author", "978-1-111-00001-0", 3, "Science");
        var bookResponse = await _client.PostAsJsonAsync("/api/books", bookDto);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookDto>();

        var memberDto = new CreateMemberDto("Flow", "Test", "flow.test@example.com");
        var memberResponse = await _client.PostAsJsonAsync("/api/members", memberDto);
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberDto>();

        // Act: Borrow
        var loanDto = new CreateLoanDto(book!.Id, member!.Id);
        var borrowResponse = await _client.PostAsJsonAsync("/api/loans", loanDto);
        borrowResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var loan = await borrowResponse.Content.ReadFromJsonAsync<LoanDto>();

        // Act: Return
        var returnResponse = await _client.PostAsync($"/api/loans/{loan!.Id}/return", null);
        returnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnResult = await returnResponse.Content.ReadFromJsonAsync<ReturnLoanDto>();

        // Assert
        returnResult.Should().NotBeNull();
        returnResult!.Id.Should().Be(loan.Id);
    }

    [Fact]
    public async Task BorrowBook_WhenNoAvailableCopies_Returns400()
    {
        // Arrange
        var bookDto = new CreateBookDto("No Copies Book", "Author", "978-1-111-00002-0", 1, "Fiction");
        var bookResponse = await _client.PostAsJsonAsync("/api/books", bookDto);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookDto>();

        var member1Dto = new CreateMemberDto("First", "Borrower", "first.borrow@example.com");
        var member1Response = await _client.PostAsJsonAsync("/api/members", member1Dto);
        var member1 = await member1Response.Content.ReadFromJsonAsync<MemberDto>();

        var member2Dto = new CreateMemberDto("Second", "Borrower", "second.borrow@example.com");
        var member2Response = await _client.PostAsJsonAsync("/api/members", member2Dto);
        var member2 = await member2Response.Content.ReadFromJsonAsync<MemberDto>();

        // First borrow succeeds
        await _client.PostAsJsonAsync("/api/loans", new CreateLoanDto(book!.Id, member1!.Id));

        // Act: Second borrow should fail
        var response = await _client.PostAsJsonAsync("/api/loans", new CreateLoanDto(book.Id, member2!.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BorrowBook_WhenMemberHas5Loans_Returns400()
    {
        // Arrange: Create member
        var memberDto = new CreateMemberDto("Max", "Loans", "max.loans@example.com");
        var memberResponse = await _client.PostAsJsonAsync("/api/members", memberDto);
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberDto>();

        // Create 6 books
        for (int i = 0; i < 6; i++)
        {
            var bDto = new CreateBookDto($"Max Loan Book {i}", "Author", $"978-1-111-1000{i}-0", 5, "History");
            var bResponse = await _client.PostAsJsonAsync("/api/books", bDto);
            var b = await bResponse.Content.ReadFromJsonAsync<BookDto>();

            var loanResponse = await _client.PostAsJsonAsync("/api/loans", new CreateLoanDto(b!.Id, member!.Id));

            if (i < 5)
            {
                loanResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            }
            else
            {
                // 6th loan should fail
                loanResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
        }
    }

    [Fact]
    public async Task GetOverdueLoans_ReturnsOnlyOverdueLoans()
    {
        // Act
        var response = await _client.GetAsync("/api/loans/overdue");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loans = await response.Content.ReadFromJsonAsync<List<LoanDto>>();
        loans.Should().NotBeNull();

        // All returned loans should have DueDate in the past and no ReturnDate
        loans!.Should().AllSatisfy(l =>
        {
            l.DueDate.Should().BeBefore(DateTime.UtcNow);
            l.ReturnDate.Should().BeNull();
        });
    }
}
