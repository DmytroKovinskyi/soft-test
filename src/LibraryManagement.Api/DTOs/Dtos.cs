namespace LibraryManagement.Api.DTOs;

public record CreateBookDto(string Title, string Author, string ISBN, int TotalCopies, string Category);

public record BookDto(int Id, string Title, string Author, string ISBN, int TotalCopies, int AvailableCopies, string Category);

public record CreateMemberDto(string FirstName, string LastName, string Email);

public record MemberDto(int Id, string FirstName, string LastName, string Email, DateTime MembershipDate, bool IsActive);

public record CreateLoanDto(int BookId, int MemberId);

public record LoanDto(int Id, int BookId, int MemberId, DateTime BorrowDate, DateTime DueDate, DateTime? ReturnDate, decimal Fine);

public record ReturnLoanDto(int Id, DateTime ReturnDate, decimal Fine);
