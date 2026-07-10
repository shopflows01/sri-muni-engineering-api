namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class TransactionFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProductId { get; set; }
    public string? TransactionType { get; set; }
    public string? SortBy { get; set; } = "date";
    public string SortDirection { get; set; } = "desc";
}
