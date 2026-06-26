namespace SriMuniEngineering_Api.Features.InspectionReports.Dtos;

public class InspectionReportResponse
{
    public Guid Id { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid DcLedgerId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? DrawingNo { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public int DcQty { get; set; }
    public int InspectedQty { get; set; }
    public string? IssueNo { get; set; }
    public string? BatchNo { get; set; }
    public List<InspectionParameter> Parameters { get; set; } = [];
    public int OkQty { get; set; }
    public int RejectedQty { get; set; }
    public int DeviationQty { get; set; }
    public string? VendorResult { get; set; }
    public string? CieResult { get; set; }
    public string? InspectedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? DownloadUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
