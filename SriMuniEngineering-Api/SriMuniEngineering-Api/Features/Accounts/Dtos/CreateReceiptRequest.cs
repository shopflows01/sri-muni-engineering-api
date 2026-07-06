namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class CreateReceiptRequest
{
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ReceiptDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Narration { get; set; }
    public List<AllocationDto> Allocations { get; set; } = [];
}
