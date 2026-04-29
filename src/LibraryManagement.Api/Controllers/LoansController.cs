using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpPost]
    public async Task<ActionResult<LoanDto>> BorrowBook([FromBody] CreateLoanDto dto)
    {
        try
        {
            var loan = await _loanService.BorrowBookAsync(dto);
            return CreatedAtAction(nameof(BorrowBook), new { id = loan.Id }, loan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/return")]
    public async Task<ActionResult<ReturnLoanDto>> ReturnBook(int id)
    {
        try
        {
            var result = await _loanService.ReturnBookAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetOverdueLoans()
    {
        var loans = await _loanService.GetOverdueLoansAsync();
        return Ok(loans);
    }
}
