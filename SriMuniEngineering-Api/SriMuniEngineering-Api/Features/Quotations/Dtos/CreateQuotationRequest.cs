using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Quotations.Dtos;

public class CreateQuotationRequest
{
    [Required]
    public string QuotationNo { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public string? Model { get; set; }
    public int NumberOff { get; set; } = 1;

    [Required]
    public List<OperationItem> Operations { get; set; } = [];

    public OtherCosts OtherCosts { get; set; } = new();
    public decimal EstimatedCostPerPart { get; set; }
    public decimal GstRate { get; set; }
}

public class OperationItem
{
    public int OpnNo { get; set; }
    public string SequenceOfOperation { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public int OutputPerHour { get; set; }
    public decimal MachineHourRate { get; set; }
    public decimal CostPerPart { get; set; }
}

public class OtherCosts
{
    public decimal ToolsCost { get; set; }
    public decimal InspectionCost { get; set; }
    public decimal OilingPackingCost { get; set; }
    public decimal OthersCost { get; set; }
}
