namespace SriMuniEngineering_Api.Domain.Entities;

public class JobWorkDCItem
{
    public Guid Id { get; set; }
    public Guid DcId { get; set; }
    public Guid ProductId { get; set; }
    public int QtySent { get; set; }
    public decimal? Rate { get; set; }
    public decimal? GstPercent { get; set; }
    public string? Remarks { get; set; }

    public JobWorkDC JobWorkDC { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<JobWorkTransaction> Transactions { get; set; } = new List<JobWorkTransaction>();
    public ICollection<InspectionReport> InspectionReports { get; set; } = new List<InspectionReport>();
}
