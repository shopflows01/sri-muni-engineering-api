using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Invoices.Dtos;

public class CreateInvoiceRequest
{
    [Required]
    public DateTime InvoiceDate { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<CreateInvoiceItemRequest> Items { get; set; } = [];

    public string? Remarks { get; set; }
    public string? DeliveryNoteNo { get; set; }
    public string? ReferenceNo { get; set; }
    public string? BuyersOrderNo { get; set; }
    public string? DispatchDocNo { get; set; }
    public string? Destination { get; set; }
    public string? TermsOfDelivery { get; set; }
    public string? AsnNo { get; set; }
    public string? EwbNo { get; set; }
}

public class CreateInvoiceItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal Rate { get; set; }

    public decimal Discount { get; set; }

    [Range(0, 100)]
    public decimal GSTPercent { get; set; }
}
