using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Api.DTOs;
using Xunit;

namespace LibraryManagement.IntegrationTests;

public class MemberIntegrationTests : IClassFixture<LibraryApiFactory>
{
    private readonly HttpClient _client;

    public MemberIntegrationTests(LibraryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMembers_ReturnsAllMembers()
    {
        // Act
        var response = await _client.GetAsync("/api/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await response.Content.ReadFromJsonAsync<List<MemberDto>>();
        members.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterMember_ReturnsCreatedMember()
    {
        // Arrange
        var dto = new CreateMemberDto("Integration", "Test", "integration.test@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/members", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var member = await response.Content.ReadFromJsonAsync<MemberDto>();
        member.Should().NotBeNull();
        member!.FirstName.Should().Be("Integration");
        member.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetMemberLoans_ReturnsCorrectHistory()
    {
        // Arrange: Create member, book, and loan
        var memberDto = new CreateMemberDto("History", "Check", "history.check@example.com");
        var memberResponse = await _client.PostAsJsonAsync("/api/members", memberDto);
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberDto>();

        var bookDto = new CreateBookDto("History Check Book", "Author", "978-1-222-33333-0", 5, "Education");
        var bookResponse = await _client.PostAsJsonAsync("/api/books", bookDto);
        var book = await bookResponse.Content.ReadFromJsonAsync<BookDto>();

        await _client.PostAsJsonAsync("/api/loans", new CreateLoanDto(book!.Id, member!.Id));

        // Act
        var response = await _client.GetAsync($"/api/members/{member.Id}/loans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loans = await response.Content.ReadFromJsonAsync<List<LoanDto>>();
        loans.Should().NotBeEmpty();
        loans!.Should().AllSatisfy(l => l.MemberId.Should().Be(member.Id));
    }

    [Fact]
    public async Task GetMemberLoans_WhenMemberNotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/members/99999/loans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
