using LibraryManagement.Api.Data;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Services;

public class LoanService : ILoanService
{
    private readonly LibraryDbContext _context;
    private const int LoanPeriodDays = 14;
    private const decimal FinePerDay = 0.50m;
    private const int MaxActiveLoans = 5;

    public LoanService(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<LoanDto> BorrowBookAsync(CreateLoanDto dto)
    {
        var book = await _context.Books.FindAsync(dto.BookId)
            ?? throw new InvalidOperationException("Book not found.");

        var member = await _context.Members.FindAsync(dto.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        // Business rule: inactive members cannot borrow books
        if (!member.IsActive)
            throw new InvalidOperationException("Inactive members cannot borrow books.");

        // Business rule: no available copies
        if (book.AvailableCopies <= 0)
            throw new InvalidOperationException("No available copies of this book.");

        // Business rule: max 5 active loans per member
        var activeLoansCount = await _context.Loans
            .CountAsync(l => l.MemberId == dto.MemberId && l.ReturnDate == null);

        if (activeLoansCount >= MaxActiveLoans)
            throw new InvalidOperationException("Member has reached the maximum number of active loans (5).");

        var loan = new Loan
        {
            BookId = dto.BookId,
            MemberId = dto.MemberId,
            BorrowDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(LoanPeriodDays),
            ReturnDate = null,
            Fine = 0
        };

        book.AvailableCopies--;

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        return new LoanDto(loan.Id, loan.BookId, loan.MemberId, loan.BorrowDate, loan.DueDate, loan.ReturnDate, loan.Fine);
    }

    public async Task<ReturnLoanDto> ReturnBookAsync(int loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == loanId)
            ?? throw new InvalidOperationException("Loan not found.");

        if (loan.ReturnDate != null)
            throw new InvalidOperationException("Book has already been returned.");

        loan.ReturnDate = DateTime.UtcNow;
        loan.Fine = CalculateFine(loan.DueDate, loan.ReturnDate.Value);
        loan.Book.AvailableCopies++;

        await _context.SaveChangesAsync();

        return new ReturnLoanDto(loan.Id, loan.ReturnDate.Value, loan.Fine);
    }

    public async Task<IEnumerable<LoanDto>> GetMemberLoansAsync(int memberId)
    {
        var memberExists = await _context.Members.AnyAsync(m => m.Id == memberId);
        if (!memberExists)
            throw new InvalidOperationException("Member not found.");

        return await _context.Loans
            .Where(l => l.MemberId == memberId)
            .Select(l => new LoanDto(l.Id, l.BookId, l.MemberId, l.BorrowDate, l.DueDate, l.ReturnDate, l.Fine))
            .ToListAsync();
    }

    public async Task<IEnumerable<LoanDto>> GetOverdueLoansAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Loans
            .Where(l => l.ReturnDate == null && l.DueDate < now)
            .Select(l => new LoanDto(l.Id, l.BookId, l.MemberId, l.BorrowDate, l.DueDate, l.ReturnDate, l.Fine))
            .ToListAsync();
    }

    public decimal CalculateFine(DateTime dueDate, DateTime returnDate)
    {
        if (returnDate <= dueDate)
            return 0m;

        var overdueDays = (returnDate.Date - dueDate.Date).Days;
        return overdueDays * FinePerDay;
    }
}
