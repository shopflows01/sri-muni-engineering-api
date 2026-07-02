namespace SriMuniEngineering_Api.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public int StateCode { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? VendorCode { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<JobWorkLedger> JobWorkLedgers { get; set; } = [];
    public ICollection<Quotation> Quotations { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<InspectionReport> InspectionReports { get; set; } = [];
}
