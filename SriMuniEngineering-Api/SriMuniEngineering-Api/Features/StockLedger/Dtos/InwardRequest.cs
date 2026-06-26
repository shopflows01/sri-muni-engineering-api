using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class InwardRequest
{
    [Required]
    public string DcNo { get; set; } = string.Empty;

    [Required]
    public DateTime DcDate { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int InwardQty { get; set; }
}
