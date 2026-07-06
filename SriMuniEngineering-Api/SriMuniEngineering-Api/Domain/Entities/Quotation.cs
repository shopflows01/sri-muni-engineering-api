namespace SriMuniEngineering_Api.Domain.Entities;

public class Quotation
{
    public Guid Id { get; set; }
    public string QuotationNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public string? Model { get; set; }
    public int NumberOff { get; set; } = 1;
    public string OperationsJson { get; set; } = "[]";
    public string OtherCostsJson { get; set; } = "{}";
    public decimal ProcessCostTotal { get; set; }
    public decimal EstimatedCostPerPart { get; set; }
    public decimal GstRate { get; set; }
    public string? StoredFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
