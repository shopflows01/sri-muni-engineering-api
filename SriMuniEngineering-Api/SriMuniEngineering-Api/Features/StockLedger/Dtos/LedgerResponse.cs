using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class LedgerResponse
{
    public Guid Id { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int InwardQty { get; set; }
    public int OutwardQty { get; set; }
    public int RejectedQty { get; set; }
    public int PendingQty => InwardQty - OutwardQty - RejectedQty;
    public LedgerStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public DateTime CreatedAt { get; set; }
}
