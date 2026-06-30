using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Features.EWayBill.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.EWayBill;

public class EWayBillService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public EWayBillService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<EWayBillRoot> BuildPayloadAsync(Guid[] invoiceIds)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Product)
            .Where(i => invoiceIds.Contains(i.Id))
            .ToListAsync();

        if (invoices.Count == 0)
            throw new KeyNotFoundException("No invoices found for the provided IDs.");

        var company = _configuration.GetSection("CompanyProfile");

        var billLists = invoices.Select((invoice, index) => new EWayBillEntry
        {
            UserGstin = company["Gstin"]!,
            SupplyType = "O",
            SubSupplyType = 1,
            SubSupplyDesc = "",
            DocType = "INV",
            DocNo = invoice.InvoiceNo,
            DocDate = invoice.Date.ToString("dd/MM/yyyy"),
            TransType = 1,

            // From (Company)
            FromGstin = company["Gstin"]!,
            FromTrdName = company["Name"]!,
            FromAddr1 = company["Address1"]!,
            FromAddr2 = company["Address2"]!,
            FromPlace = company["City"]!,
            FromPincode = int.Parse(company["Pincode"]!),
            FromStateCode = int.Parse(company["StateCode"]!),
            ActualFromStateCode = int.Parse(company["StateCode"]!),

            // To (Customer)
            ToGstin = invoice.Customer.GSTIN,
            ToTrdName = invoice.Customer.Name,
            ToAddr1 = invoice.Customer.BillingAddress,
            ToAddr2 = invoice.Customer.ShippingAddress,
            ToPlace = invoice.Customer.StateName,
            ToPincode = int.Parse(invoice.Customer.Pincode),
            ToStateCode = invoice.Customer.StateCode,
            ActualToStateCode = invoice.Customer.StateCode,

            // Values
            TotalValue = invoice.TaxableValue,
            CgstValue = invoice.CgstAmount,
            SgstValue = invoice.SgstAmount,
            IgstValue = invoice.IgstAmount,
            CessValue = 0,
            TotNonAdvolVal = 0,
            OthValue = 0,
            TotInvValue = invoice.TotalAmount,

            // Transport
            TransMode = 1,
            TransDistance = 0,
            TransporterName = "",
            TransporterId = "",
            TransDocNo = "",
            TransDocDate = invoice.Date.ToString("dd/MM/yyyy"),
            VehicleNo = "",
            VehicleType = "R",

            // HSN
            MainHsnCode = invoice.Product.HsnSac,

            // Item list
            ItemList =
            [
                new EWayBillItem
                {
                    ItemNo = 1,
                    ProductName = invoice.Product.PartName,
                    ProductDesc = invoice.Product.PartDescription ?? invoice.Product.PartName,
                    HsnCode = invoice.Product.HsnSac,
                    Quantity = invoice.Quantity,
                    QtyUnit = invoice.Product.Unit.ToUpper() switch
                    {
                        "NOS" => "NOS",
                        "KGS" => "KGS",
                        "PCS" => "PCS",
                        _ => "NOS"
                    },
                    TaxableAmount = invoice.TaxableValue,
                    SgstRate = invoice.SgstRate,
                    CgstRate = invoice.CgstRate,
                    IgstRate = invoice.IgstRate,
                    CessRate = 0,
                    CessNonAdvol = 0
                }
            ]
        }).ToList();

        return new EWayBillRoot
        {
            Version = "1.0.1118",
            BillLists = billLists
        };
    }
}
