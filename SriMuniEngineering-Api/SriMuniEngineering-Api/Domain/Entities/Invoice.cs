namespace SriMuniEngineering_Api.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public int InvoiceSequence { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string? AmountInWords { get; set; }
    public string? Remarks { get; set; }
    public string? DeliveryNoteNo { get; set; }
    public string? ReferenceNo { get; set; }
    public string? BuyersOrderNo { get; set; }
    public string? DispatchDocNo { get; set; }
    public string? Destination { get; set; }
    public string? TermsOfDelivery { get; set; }
    public string? AsnNo { get; set; }
    public string? EwbNo { get; set; }
    public string? StoredFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<InvoiceItem> Items { get; set; } = [];
    public ICollection<InspectionReport> InspectionReports { get; set; } = [];
}
