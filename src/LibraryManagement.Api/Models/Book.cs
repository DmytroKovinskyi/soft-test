namespace LibraryManagement.Api.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Category { get; set; } = string.Empty;
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
