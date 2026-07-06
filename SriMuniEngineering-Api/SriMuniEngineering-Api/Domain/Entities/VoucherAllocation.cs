namespace SriMuniEngineering_Api.Domain.Entities;

public class VoucherAllocation
{
    public Guid Id { get; set; }
    public Guid VoucherEntryId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime AllocationDate { get; set; } = DateTime.UtcNow;
    public string? Remarks { get; set; }

    // Navigation properties
    public VoucherEntry VoucherEntry { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
}
