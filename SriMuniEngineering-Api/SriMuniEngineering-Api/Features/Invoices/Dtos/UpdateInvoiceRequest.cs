using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Invoices.Dtos;

public class UpdateInvoiceRequest
{
    [Required]
    public DateTime InvoiceDate { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<CreateInvoiceItemRequest> Items { get; set; } = [];

    public string? Remarks { get; set; }
    public Guid? DcLedgerId { get; set; }
    public string? DeliveryNoteNo { get; set; }
    public DateTime? DcDate { get; set; }
    public string? ReferenceNo { get; set; }
    public string? BuyersOrderNo { get; set; }
    public string? DispatchDocNo { get; set; }
    public string? Destination { get; set; }
    public string? TermsOfDelivery { get; set; }
    public string? AsnNo { get; set; }
    public string? EwbNo { get; set; }
}
