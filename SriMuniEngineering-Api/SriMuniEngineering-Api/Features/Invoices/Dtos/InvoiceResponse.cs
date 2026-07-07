namespace SriMuniEngineering_Api.Features.Invoices.Dtos;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public int InvoiceSequence { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string? AmountInWords { get; set; }
    public string? Remarks { get; set; }
    public string? DeliveryNoteNo { get; set; }
    public DateTime? DcDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? BuyersOrderNo { get; set; }
    public string? DispatchDocNo { get; set; }
    public string? Destination { get; set; }
    public string? TermsOfDelivery { get; set; }
    public string? AsnNo { get; set; }
    public string? EwbNo { get; set; }
    public string? DownloadUrl { get; set; }
    public List<InvoiceItemResponse> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class InvoiceItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string HsnSac { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HsnCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal GSTPercent { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal Amount { get; set; }
}

public class NextInvoiceNumberResponse
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int InvoiceSequence { get; set; }
    public string FinancialYear { get; set; } = string.Empty;
}
