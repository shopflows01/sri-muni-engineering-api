using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class UpdateJobWorkDCRequest
{
    [Required]
    public string DcNo { get; set; } = string.Empty;

    [Required]
    public DateTime DcDate { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    public string? Remarks { get; set; }

    [Required]
    [MinLength(1)]
    public List<UpdateJobWorkDCItemRequest> Items { get; set; } = new();
}

public class UpdateJobWorkDCItemRequest
{
    public Guid? Id { get; set; } // If null, it's a new item

    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int QtySent { get; set; }

    public decimal? Rate { get; set; }
    
    public decimal? GstPercent { get; set; }
    
    public string? Remarks { get; set; }
}
