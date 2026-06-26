namespace SriMuniEngineering_Api.Features.StockLedger.Dtos;

public class StockExportQuery
{
    public string Period { get; set; } = "monthly"; // weekly, monthly
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
