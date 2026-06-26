using System.Text.Json.Serialization;

namespace SriMuniEngineering_Api.Features.EWayBill.Dtos;

public class EWayBillRoot
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.1118";

    [JsonPropertyName("billLists")]
    public List<EWayBillEntry> BillLists { get; set; } = [];
}

public class EWayBillEntry
{
    [JsonPropertyName("userGstin")]
    public string UserGstin { get; set; } = string.Empty;

    [JsonPropertyName("supplyType")]
    public string SupplyType { get; set; } = "O";

    [JsonPropertyName("subSupplyType")]
    public int SubSupplyType { get; set; } = 1;

    [JsonPropertyName("subSupplyDesc")]
    public string SubSupplyDesc { get; set; } = string.Empty;

    [JsonPropertyName("docType")]
    public string DocType { get; set; } = "INV";

    [JsonPropertyName("docNo")]
    public string DocNo { get; set; } = string.Empty;

    [JsonPropertyName("docDate")]
    public string DocDate { get; set; } = string.Empty;

    [JsonPropertyName("transType")]
    public int TransType { get; set; } = 1;

    [JsonPropertyName("fromGstin")]
    public string FromGstin { get; set; } = string.Empty;

    [JsonPropertyName("fromTrdName")]
    public string FromTrdName { get; set; } = string.Empty;

    [JsonPropertyName("fromAddr1")]
    public string FromAddr1 { get; set; } = string.Empty;

    [JsonPropertyName("fromAddr2")]
    public string FromAddr2 { get; set; } = string.Empty;

    [JsonPropertyName("fromPlace")]
    public string FromPlace { get; set; } = string.Empty;

    [JsonPropertyName("fromPincode")]
    public int FromPincode { get; set; }

    [JsonPropertyName("fromStateCode")]
    public int FromStateCode { get; set; }

    [JsonPropertyName("actualFromStateCode")]
    public int ActualFromStateCode { get; set; }

    [JsonPropertyName("toGstin")]
    public string ToGstin { get; set; } = string.Empty;

    [JsonPropertyName("toTrdName")]
    public string ToTrdName { get; set; } = string.Empty;

    [JsonPropertyName("toAddr1")]
    public string ToAddr1 { get; set; } = string.Empty;

    [JsonPropertyName("toAddr2")]
    public string ToAddr2 { get; set; } = string.Empty;

    [JsonPropertyName("toPlace")]
    public string ToPlace { get; set; } = string.Empty;

    [JsonPropertyName("toPincode")]
    public int ToPincode { get; set; }

    [JsonPropertyName("toStateCode")]
    public int ToStateCode { get; set; }

    [JsonPropertyName("actualToStateCode")]
    public int ActualToStateCode { get; set; }

    [JsonPropertyName("totalValue")]
    public decimal TotalValue { get; set; }

    [JsonPropertyName("cgstValue")]
    public decimal CgstValue { get; set; }

    [JsonPropertyName("sgstValue")]
    public decimal SgstValue { get; set; }

    [JsonPropertyName("igstValue")]
    public decimal IgstValue { get; set; }

    [JsonPropertyName("cessValue")]
    public decimal CessValue { get; set; }

    [JsonPropertyName("TotNonAdvolVal")]
    public decimal TotNonAdvolVal { get; set; }

    [JsonPropertyName("OthValue")]
    public decimal OthValue { get; set; }

    [JsonPropertyName("totInvValue")]
    public decimal TotInvValue { get; set; }

    [JsonPropertyName("transMode")]
    public int TransMode { get; set; } = 1;

    [JsonPropertyName("transDistance")]
    public int TransDistance { get; set; }

    [JsonPropertyName("transporterName")]
    public string TransporterName { get; set; } = string.Empty;

    [JsonPropertyName("transporterId")]
    public string TransporterId { get; set; } = string.Empty;

    [JsonPropertyName("transDocNo")]
    public string TransDocNo { get; set; } = string.Empty;

    [JsonPropertyName("transDocDate")]
    public string TransDocDate { get; set; } = string.Empty;

    [JsonPropertyName("vehicleNo")]
    public string VehicleNo { get; set; } = string.Empty;

    [JsonPropertyName("vehicleType")]
    public string VehicleType { get; set; } = "R";

    [JsonPropertyName("mainHsnCode")]
    public string MainHsnCode { get; set; } = string.Empty;

    [JsonPropertyName("itemList")]
    public List<EWayBillItem> ItemList { get; set; } = [];
}

public class EWayBillItem
{
    [JsonPropertyName("itemNo")]
    public int ItemNo { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("productDesc")]
    public string ProductDesc { get; set; } = string.Empty;

    [JsonPropertyName("hsnCode")]
    public string HsnCode { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("qtyUnit")]
    public string QtyUnit { get; set; } = string.Empty;

    [JsonPropertyName("taxableAmount")]
    public decimal TaxableAmount { get; set; }

    [JsonPropertyName("sgstRate")]
    public decimal SgstRate { get; set; }

    [JsonPropertyName("cgstRate")]
    public decimal CgstRate { get; set; }

    [JsonPropertyName("igstRate")]
    public decimal IgstRate { get; set; }

    [JsonPropertyName("cessRate")]
    public decimal CessRate { get; set; }

    [JsonPropertyName("cessNonAdvol")]
    public decimal CessNonAdvol { get; set; }
}
