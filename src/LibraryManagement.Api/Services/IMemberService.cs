using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services;

public interface IMemberService
{
    Task<IEnumerable<MemberDto>> GetAllMembersAsync();
    Task<MemberDto> RegisterMemberAsync(CreateMemberDto dto);
}
