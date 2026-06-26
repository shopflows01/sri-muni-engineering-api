namespace SriMuniEngineering_Api.Features.Quotations.Dtos;

public class QuotationResponse
{
    public Guid Id { get; set; }
    public string QuotationNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int NumberOff { get; set; }
    public List<OperationItem> Operations { get; set; } = [];
    public OtherCosts OtherCosts { get; set; } = new();
    public decimal ProcessCostTotal { get; set; }
    public decimal EstimatedCostPerPart { get; set; }
    public decimal GstRate { get; set; }
    public string? DownloadUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
