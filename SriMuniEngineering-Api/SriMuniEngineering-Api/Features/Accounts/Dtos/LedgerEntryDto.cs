namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class LedgerEntryDto
{
    public DateTime Date { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public string VoucherType { get; set; } = string.Empty;
    public string? Narration { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}
