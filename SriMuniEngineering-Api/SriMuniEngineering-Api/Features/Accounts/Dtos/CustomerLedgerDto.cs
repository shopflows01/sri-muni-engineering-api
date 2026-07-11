using SriMuniEngineering_Api.Common.Dtos;

namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class CustomerLedgerDto
{
    public Guid CustomerId { get; set; }
    public string LedgerNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public string OpeningBalanceType { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal AdvanceAmount { get; set; }
    public PagedResponse<LedgerEntryDto> Entries { get; set; } = new PagedResponse<LedgerEntryDto>();
}
