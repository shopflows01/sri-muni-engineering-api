using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Domain.Entities;

public class CustomerLedger
{
    public Guid Id { get; set; }
    public string LedgerNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal OpeningBalance { get; set; }
    public BalanceType OpeningBalanceType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<VoucherEntry> VoucherEntries { get; set; } = [];
}
