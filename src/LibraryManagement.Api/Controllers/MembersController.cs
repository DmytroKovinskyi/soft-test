using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ILoanService _loanService;

    public MembersController(IMemberService memberService, ILoanService loanService)
    {
        _memberService = memberService;
        _loanService = loanService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetMembers()
    {
        var members = await _memberService.GetAllMembersAsync();
        return Ok(members);
    }

    [HttpPost]
    public async Task<ActionResult<MemberDto>> RegisterMember([FromBody] CreateMemberDto dto)
    {
        var member = await _memberService.RegisterMemberAsync(dto);
        return CreatedAtAction(nameof(GetMembers), new { id = member.Id }, member);
    }

    [HttpGet("{id}/loans")]
    public async Task<ActionResult<IEnumerable<LoanDto>>> GetMemberLoans(int id)
    {
        try
        {
            var loans = await _loanService.GetMemberLoansAsync(id);
            return Ok(loans);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
