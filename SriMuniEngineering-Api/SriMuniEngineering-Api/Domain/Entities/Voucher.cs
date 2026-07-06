using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Domain.Entities;

public class Voucher
{
    public Guid Id { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public DateTime VoucherDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Narration { get; set; }
    public VoucherStatus Status { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<VoucherEntry> Entries { get; set; } = [];
}
