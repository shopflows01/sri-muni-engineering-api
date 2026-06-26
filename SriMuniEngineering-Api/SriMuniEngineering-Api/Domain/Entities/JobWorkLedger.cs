using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Domain.Entities;

public class JobWorkLedger
{
    public Guid Id { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public int InwardQty { get; set; }
    public int OutwardQty { get; set; }
    public int RejectedQty { get; set; }
    public LedgerStatus Status { get; set; } = LedgerStatus.InProgress;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<InspectionReport> InspectionReports { get; set; } = [];
}
