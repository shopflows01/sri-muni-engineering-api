namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class InvoiceSummaryDto
{
    public int TotalInvoices { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
    public int PartiallyPaidCount { get; set; }
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalOutstanding { get; set; }
}
