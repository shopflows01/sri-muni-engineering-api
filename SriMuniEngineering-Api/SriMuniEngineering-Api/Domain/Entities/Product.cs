namespace SriMuniEngineering_Api.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? PartDescription { get; set; }
    public decimal BasePricePerUnit { get; set; }
    public decimal RatePerItem { get; set; }
    public decimal GstPercent { get; set; }
    public string HsnSac { get; set; } = string.Empty;
    public string Unit { get; set; } = "Nos";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<JobWorkDCItem> JobWorkDCItems { get; set; } = [];
    public ICollection<Quotation> Quotations { get; set; } = [];
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = [];
    public ICollection<InspectionReport> InspectionReports { get; set; } = [];
    public ICollection<DeliveryChallanItem> DeliveryChallanItems { get; set; } = [];
}
