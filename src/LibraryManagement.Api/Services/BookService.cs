using LibraryManagement.Api.Data;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public class BookService : IBookService
{
    private readonly LibraryDbContext _context;

    public BookService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BookDto>> GetAllBooksAsync(string? category, bool? available)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        if (available.HasValue)
        {
            query = available.Value
                ? query.Where(b => b.AvailableCopies > 0)
                : query.Where(b => b.AvailableCopies == 0);
        }

        return await query.Select(b => new BookDto(
            b.Id, b.Title, b.Author, b.ISBN, b.TotalCopies, b.AvailableCopies, b.Category
        )).ToListAsync();
    }

    public async Task<BookDto> AddBookAsync(CreateBookDto dto)
    {
        var book = new Book
        {
            Title = dto.Title,
            Author = dto.Author,
            ISBN = dto.ISBN,
            TotalCopies = dto.TotalCopies,
            AvailableCopies = dto.TotalCopies,
            Category = dto.Category
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        return new BookDto(book.Id, book.Title, book.Author, book.ISBN, book.TotalCopies, book.AvailableCopies, book.Category);
    }
}
