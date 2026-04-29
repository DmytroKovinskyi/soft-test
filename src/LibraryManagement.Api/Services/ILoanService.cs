using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services;

public interface ILoanService
{
    Task<LoanDto> BorrowBookAsync(CreateLoanDto dto);
    Task<ReturnLoanDto> ReturnBookAsync(int loanId);
    Task<IEnumerable<LoanDto>> GetMemberLoansAsync(int memberId);
    Task<IEnumerable<LoanDto>> GetOverdueLoansAsync();
    decimal CalculateFine(DateTime dueDate, DateTime returnDate);
}
