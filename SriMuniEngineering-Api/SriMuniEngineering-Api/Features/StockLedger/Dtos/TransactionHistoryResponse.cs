namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class TransactionHistoryResponse
{
    public Guid TransactionId { get; set; }
    public Guid DcItemId { get; set; }
    public Guid DcId { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
}
