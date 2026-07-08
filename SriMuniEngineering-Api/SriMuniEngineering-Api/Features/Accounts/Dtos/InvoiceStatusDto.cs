namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class InvoiceStatusDto
{
    public Guid InvoiceId { get; set; }
    public Guid CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; } = string.Empty;
}
