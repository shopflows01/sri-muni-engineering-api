using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class OutwardRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int OutwardQty { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int RejectedQty { get; set; }
}
