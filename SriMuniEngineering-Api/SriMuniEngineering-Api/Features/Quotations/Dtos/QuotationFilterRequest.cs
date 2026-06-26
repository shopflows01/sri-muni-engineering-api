using SriMuniEngineering_Api.Common;

namespace SriMuniEngineering_Api.Features.Quotations.Dtos;

public class QuotationFilterRequest : PaginatedRequest
{
    public string? QuotationNo { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProductId { get; set; }
    public string? CustomerName { get; set; }
    public string? PartNo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
