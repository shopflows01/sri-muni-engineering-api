using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Domain.Entities;

public class DeliveryChallan
{
    public Guid Id { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public DateTime DcDate { get; set; }
    
    public string? YourDcNo { get; set; }
    public DateTime? YourDcDate { get; set; }
    public string? PoNo { get; set; }
    public string? Remarks { get; set; }
    
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public Customer Customer { get; set; } = null!;
    public ICollection<DeliveryChallanItem> Items { get; set; } = new List<DeliveryChallanItem>();
}
