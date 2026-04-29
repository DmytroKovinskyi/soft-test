using LibraryManagement.Api.Data;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public class MemberService : IMemberService
{
    private readonly LibraryDbContext _context;

    public MemberService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MemberDto>> GetAllMembersAsync()
    {
        return await _context.Members.Select(m => new MemberDto(
            m.Id, m.FirstName, m.LastName, m.Email, m.MembershipDate, m.IsActive
        )).ToListAsync();
    }

    public async Task<MemberDto> RegisterMemberAsync(CreateMemberDto dto)
    {
        var member = new Member
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        };

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        return new MemberDto(member.Id, member.FirstName, member.LastName, member.Email, member.MembershipDate, member.IsActive);
    }
}
