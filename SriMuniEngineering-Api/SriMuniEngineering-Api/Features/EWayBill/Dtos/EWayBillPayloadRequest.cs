using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.EWayBill.Dtos;

public class EWayBillPayloadRequest
{
    [Required]
    [MinLength(1)]
    public Guid[] InvoiceIds { get; set; } = [];
}
