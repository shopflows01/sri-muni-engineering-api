namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class CustomerLedgerDto
{
    public Guid CustomerId { get; set; }
    public string LedgerNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public string OpeningBalanceType { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public List<LedgerEntryDto> Entries { get; set; } = [];
}
