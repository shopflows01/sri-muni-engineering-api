using System.ComponentModel.DataAnnotations;
using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class TransactionRequest
{
    [Required]
    public TransactionType TransactionType { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
}
