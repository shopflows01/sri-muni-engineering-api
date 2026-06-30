namespace SriMuniEngineering_Api.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid DcLedgerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal IgstRate { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CgstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstRate { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? AmountInWords { get; set; }
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
    public JobWorkLedger DcLedger { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<InspectionReport> InspectionReports { get; set; } = [];
}
