namespace SriMuniEngineering_Api.Domain.Entities;

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string? Description { get; set; }
    public string? HsnCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal GSTPercent { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal Amount { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
