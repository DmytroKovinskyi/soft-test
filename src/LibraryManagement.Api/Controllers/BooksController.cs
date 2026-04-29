using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(
        [FromQuery] string? category,
        [FromQuery] bool? available)
    {
        var books = await _bookService.GetAllBooksAsync(category, available);
        return Ok(books);
    }

    [HttpPost]
    public async Task<ActionResult<BookDto>> AddBook([FromBody] CreateBookDto dto)
    {
        var book = await _bookService.AddBookAsync(dto);
        return CreatedAtAction(nameof(GetBooks), new { id = book.Id }, book);
    }
}
