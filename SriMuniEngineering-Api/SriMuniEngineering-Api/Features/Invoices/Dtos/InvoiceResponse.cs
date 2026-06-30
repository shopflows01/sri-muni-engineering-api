namespace SriMuniEngineering_Api.Features.Invoices.Dtos;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid DcLedgerId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string HsnSac { get; set; } = string.Empty;
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
    public string? DownloadUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
