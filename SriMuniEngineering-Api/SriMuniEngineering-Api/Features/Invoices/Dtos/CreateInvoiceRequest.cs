using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Invoices.Dtos;

public class CreateInvoiceRequest
{
    [Required]
    public string InvoiceNo { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public Guid DcLedgerId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue)]
    public decimal Rate { get; set; }

    public decimal IgstRate { get; set; }
    public decimal CgstRate { get; set; }
    public decimal SgstRate { get; set; }
    public string? DeliveryNoteNo { get; set; }
    public string? ReferenceNo { get; set; }
    public string? BuyersOrderNo { get; set; }
    public string? DispatchDocNo { get; set; }
    public string? Destination { get; set; }
    public string? TermsOfDelivery { get; set; }
    public string? AsnNo { get; set; }
    public string? EwbNo { get; set; }
}
