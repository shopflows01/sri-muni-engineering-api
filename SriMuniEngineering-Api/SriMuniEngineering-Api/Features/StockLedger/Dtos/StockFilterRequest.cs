using SriMuniEngineering_Api.Common;
using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class StockFilterRequest : PaginatedRequest
{
    public string? DcNo { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProductId { get; set; }
    public LedgerStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? CustomerName { get; set; }
    public string? PartNo { get; set; }
}
