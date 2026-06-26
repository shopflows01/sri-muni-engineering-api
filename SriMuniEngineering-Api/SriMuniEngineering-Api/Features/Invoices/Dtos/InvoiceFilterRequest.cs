using SriMuniEngineering_Api.Common;

namespace SriMuniEngineering_Api.Features.Invoices.Dtos;

public class InvoiceFilterRequest : PaginatedRequest
{
    public string? InvoiceNo { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProductId { get; set; }
    public string? CustomerName { get; set; }
    public string? PartNo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsLocked { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}
