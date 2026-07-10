using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Domain.Entities;

public class JobWorkDC
{
    public Guid Id { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public DateTime DcDate { get; set; }
    public string? Remarks { get; set; }
    public LedgerStatus Status { get; set; } = LedgerStatus.InProgress;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public Customer Customer { get; set; } = null!;
    public ICollection<JobWorkDCItem> Items { get; set; } = new List<JobWorkDCItem>();
}
