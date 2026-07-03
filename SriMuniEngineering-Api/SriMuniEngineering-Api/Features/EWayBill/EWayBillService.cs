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
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .Where(i => invoiceIds.Contains(i.Id))
            .ToListAsync();

        if (invoices.Count == 0)
            throw new KeyNotFoundException("No invoices found for the provided IDs.");

        var company = _configuration.GetSection("CompanyProfile");

        var billLists = invoices.Select((invoice, index) =>
        {
            // Aggregate GST values from items
            var totalTaxableValue = invoice.Items.Sum(i => (i.Quantity * i.Rate) - i.Discount);
            var totalGstAmount = invoice.Items.Sum(i => i.GSTAmount);

            // Use the first item's HSN as the main HSN code
            var mainHsnCode = invoice.Items.FirstOrDefault()?.Product.HsnSac ?? "";

            return new EWayBillEntry
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

                // Values — Split GST equally between CGST/SGST for intra-state
                TotalValue = totalTaxableValue,
                CgstValue = totalGstAmount / 2,
                SgstValue = totalGstAmount / 2,
                IgstValue = 0,
                CessValue = 0,
                TotNonAdvolVal = 0,
                OthValue = 0,
                TotInvValue = invoice.GrandTotal,

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
                MainHsnCode = mainHsnCode,

                // Item list
                ItemList = invoice.Items.Select((item, idx) =>
                {
                    var taxableAmount = (item.Quantity * item.Rate) - item.Discount;
                    return new EWayBillItem
                    {
                        ItemNo = idx + 1,
                        ProductName = item.Product.PartName,
                        ProductDesc = item.Description ?? item.Product.PartDescription ?? item.Product.PartName,
                        HsnCode = item.Product.HsnSac,
                        Quantity = item.Quantity,
                        QtyUnit = item.Product.Unit.ToUpper() switch
                        {
                            "NOS" => "NOS",
                            "KGS" => "KGS",
                            "PCS" => "PCS",
                            _ => "NOS"
                        },
                        TaxableAmount = taxableAmount,
                        SgstRate = item.GSTPercent / 2,
                        CgstRate = item.GSTPercent / 2,
                        IgstRate = 0,
                        CessRate = 0,
                        CessNonAdvol = 0
                    };
                }).ToList()
            };
        }).ToList();

        return new EWayBillRoot
        {
            Version = "1.0.1118",
            BillLists = billLists
        };
    }
}
