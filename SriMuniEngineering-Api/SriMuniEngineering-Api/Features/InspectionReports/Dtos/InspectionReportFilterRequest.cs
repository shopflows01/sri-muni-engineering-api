using SriMuniEngineering_Api.Common;

namespace SriMuniEngineering_Api.Features.InspectionReports.Dtos;

public class InspectionReportFilterRequest : PaginatedRequest
{
    public string? DcNo { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProductId { get; set; }
    public string? CustomerName { get; set; }
    public string? PartNo { get; set; }
    public string? Operation { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? InvoiceId { get; set; }
}
