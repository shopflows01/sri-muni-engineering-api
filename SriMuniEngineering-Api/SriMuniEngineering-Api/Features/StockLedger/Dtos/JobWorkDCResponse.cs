using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class JobWorkDCResponse
{
    public Guid Id { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public LedgerStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public DateTime CreatedAt { get; set; }
    public List<JobWorkDCItemResponse> Items { get; set; } = new();
}

public class JobWorkDCItemResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int QtySent { get; set; }
    public decimal? Rate { get; set; }
    public decimal? GstPercent { get; set; }
    public string? Remarks { get; set; }
    
    // Aggregated from transactions
    public int InwardQty { get; set; }
    public int OutwardQty { get; set; }
    public int RejectedQty { get; set; }
    public int PendingQty => InwardQty - OutwardQty - RejectedQty;
    
    public List<JobWorkTransactionResponse> Transactions { get; set; } = new();
}

public class JobWorkTransactionResponse
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public string TransactionTypeText => TransactionType.ToString();
    public int Quantity { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
}
