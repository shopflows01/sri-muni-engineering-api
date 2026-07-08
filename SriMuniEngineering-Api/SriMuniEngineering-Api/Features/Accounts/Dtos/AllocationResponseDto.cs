namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class AllocationResponseDto
{
    public Guid Id { get; set; }
    public Guid ReceiptVoucherId { get; set; }
    public string ReceiptVoucherNumber { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
    public DateTime AllocationDate { get; set; }
}
