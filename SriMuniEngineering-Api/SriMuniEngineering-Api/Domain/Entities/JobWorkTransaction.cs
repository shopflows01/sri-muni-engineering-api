using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Domain.Entities;

public class JobWorkTransaction
{
    public Guid Id { get; set; }
    public Guid DcItemId { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }

    public JobWorkDCItem DcItem { get; set; } = null!;
}
