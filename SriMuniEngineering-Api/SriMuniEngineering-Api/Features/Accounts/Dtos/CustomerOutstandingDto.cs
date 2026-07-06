namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class CustomerOutstandingDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Outstanding { get; set; }
    public decimal AdvanceBalance { get; set; }
}
