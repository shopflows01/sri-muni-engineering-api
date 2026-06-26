using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Invoices.Dtos;

public class ShippingMetadataRequest
{
    [Required]
    public string AsnNo { get; set; } = string.Empty;

    [Required]
    public string TransportDetails { get; set; } = string.Empty;

    [Required]
    public string EwbNo { get; set; } = string.Empty;
}
