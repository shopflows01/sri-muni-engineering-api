using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.InspectionReports.Dtos;

public class CreateInspectionReportRequest
{
    [Required]
    public Guid DcLedgerId { get; set; }

    public Guid? InvoiceId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public string? DrawingNo { get; set; }

    [Required]
    public string Operation { get; set; } = string.Empty;

    [Required]
    public string DcNo { get; set; } = string.Empty;

    [Required]
    public DateTime DcDate { get; set; }

    [Required]
    public int DcQty { get; set; }

    [Required]
    public int InspectedQty { get; set; }

    public string? IssueNo { get; set; }
    public string? BatchNo { get; set; }

    [Required]
    public List<InspectionParameter> Parameters { get; set; } = [];

    public int OkQty { get; set; }
    public int RejectedQty { get; set; }
    public int DeviationQty { get; set; }
    public string? VendorResult { get; set; }
    public string? CieResult { get; set; }
    public string? InspectedBy { get; set; }
    public string? ApprovedBy { get; set; }
}

public class InspectionParameter
{
    public int SlNo { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string DrawingSpecification { get; set; } = string.Empty;
    public string MeasurementTechnics { get; set; } = string.Empty;
    public List<string> ActualDimensions { get; set; } = [];
    public string? Remarks { get; set; }
}
