using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services;

public interface IBookService
{
    Task<IEnumerable<BookDto>> GetAllBooksAsync(string? category, bool? available);
    Task<BookDto> AddBookAsync(CreateBookDto dto);
}
