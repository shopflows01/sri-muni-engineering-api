namespace SriMuniEngineering_Api.Domain.Entities;

public class InspectionReport
{
    public Guid Id { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid DcLedgerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public string? DrawingNo { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public int DcQty { get; set; }
    public int InspectedQty { get; set; }
    public string? IssueNo { get; set; }
    public string? BatchNo { get; set; }
    public string ParametersJson { get; set; } = "[]";
    public int OkQty { get; set; }
    public int RejectedQty { get; set; }
    public int DeviationQty { get; set; }
    public string? VendorResult { get; set; }
    public string? CieResult { get; set; }
    public string? InspectedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? StoredFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Invoice? Invoice { get; set; }
    public JobWorkLedger DcLedger { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
