namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class CustomerOutstandingDetailDto
{
    public Guid CustomerId { get; set; }
    public List<InvoiceStatusDto> Invoices { get; set; } = [];
}
