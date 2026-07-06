using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Domain.Entities;

public class VoucherEntry
{
    public Guid Id { get; set; }
    public Guid VoucherId { get; set; }
    public Guid? CustomerLedgerId { get; set; }
    public SystemAccountType? SystemAccount { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Voucher Voucher { get; set; } = null!;
    public CustomerLedger? CustomerLedger { get; set; }
    public ICollection<VoucherAllocation> Allocations { get; set; } = [];
}
