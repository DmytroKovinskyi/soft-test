using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryManagement.Api.DTOs;
using Xunit;

namespace LibraryManagement.IntegrationTests;

[Collection("Integration Tests Collection")]
public class BookIntegrationTests
{
    private readonly HttpClient _client;

    public BookIntegrationTests(LibraryApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBooks_ReturnsAllBooks()
    {
        // Act
        var response = await _client.GetAsync("/api/books");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>();
        books.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBooks_FilterByCategory_ReturnsFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/books?category=Fiction");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>();
        books.Should().NotBeNull();
        books!.Should().AllSatisfy(b => b.Category.Should().Be("Fiction"));
    }

    [Fact]
    public async Task GetBooks_FilterByAvailability_ReturnsOnlyAvailable()
    {
        // Act
        var response = await _client.GetAsync("/api/books?available=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>();
        books.Should().NotBeNull();
        books!.Should().AllSatisfy(b => b.AvailableCopies.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task AddBook_ReturnsCreatedBook()
    {
        // Arrange
        var newBook = new CreateBookDto("Test Book Integration", "Test Author", "978-9-999-99999-9", 5, "Technology");

        // Act
        var response = await _client.PostAsJsonAsync("/api/books", newBook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = await response.Content.ReadFromJsonAsync<BookDto>();
        book.Should().NotBeNull();
        book!.Title.Should().Be("Test Book Integration");
        book.AvailableCopies.Should().Be(5);
    }
}
