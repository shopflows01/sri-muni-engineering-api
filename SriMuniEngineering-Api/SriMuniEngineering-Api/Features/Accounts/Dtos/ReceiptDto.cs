namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class ReceiptDto
{
    public Guid VoucherId { get; set; }
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount => Amount - AllocatedAmount;
    public string? ReferenceNumber { get; set; }
    public string? Narration { get; set; }
    public string VoucherType { get; set; } = string.Empty;
}
