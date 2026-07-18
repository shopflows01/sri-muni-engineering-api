namespace SriMuniEngineering_Api.Domain.Entities;

public class DeliveryChallanItem
{
    public Guid Id { get; set; }
    public Guid DeliveryChallanId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Remarks { get; set; }

    public DeliveryChallan DeliveryChallan { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
