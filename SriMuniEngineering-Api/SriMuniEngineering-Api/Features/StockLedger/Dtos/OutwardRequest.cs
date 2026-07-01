using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class OutwardRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int OutwardQty { get; set; }
}
