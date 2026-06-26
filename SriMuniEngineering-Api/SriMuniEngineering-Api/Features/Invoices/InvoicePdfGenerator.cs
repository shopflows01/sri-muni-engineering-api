using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SriMuniEngineering_Api.Domain.Entities;

namespace SriMuniEngineering_Api.Features.Invoices;

public static class InvoicePdfGenerator
{
    private static readonly string LogoPath = Path.Combine(
        AppContext.BaseDirectory, "Assets", "sme-logo.png");

    public static byte[] Generate(Invoice invoice, IConfigurationSection companyProfile)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => ComposeHeader(header));
                page.Content().Element(content => ComposeContent(content, invoice, companyProfile));
                page.Footer().AlignCenter().Text("SUBJECT TO HOSUR JURISDICTION").FontSize(8).Bold();
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Logo on the left side of header
                if (File.Exists(LogoPath))
                {
                    row.ConstantItem(50).AlignMiddle().Image(LogoPath);
                }

                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("TAX INVOICE").Bold().FontSize(14);
                    c.Item().AlignCenter().Text("(ORIGINAL FOR RECIPIENT)").FontSize(8).Italic();
                });

                // Spacer to balance the logo width
                row.ConstantItem(50);
            });
            col.Item().PaddingBottom(5).LineHorizontal(1);
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice, IConfigurationSection company)
    {
        container.Column(column =>
        {
            // Seller & Invoice details row
            column.Item().Border(1).Row(row =>
            {
                // Left: Seller details
                row.RelativeItem().BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Text(company["Name"]!).Bold().FontSize(10);
                    col.Item().Text($"{company["Address1"]}").FontSize(8);
                    col.Item().Text($"{company["Address2"]}").FontSize(8);
                    col.Item().Text($"{company["City"]} - {company["Pincode"]}").FontSize(8);
                    col.Item().Text($"GSTIN/UIN: {company["Gstin"]}").FontSize(8);
                    col.Item().Text($"State Name: {company["State"]}, Code: {company["StateCode"]}").FontSize(8);
                    col.Item().Text($"Contact: {company["Phone"]}, {company["AltPhone"]}").FontSize(8);
                    col.Item().Text($"E-Mail: {company["Email"]}").FontSize(8);
                });

                // Right: Invoice metadata
                row.RelativeItem().Padding(5).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Invoice No.").FontSize(8);
                        r.RelativeItem().Text(invoice.InvoiceNo).Bold().FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Dated").FontSize(8);
                        r.RelativeItem().Text(invoice.Date.ToString("dd-MMM-yy")).Bold().FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Delivery Note").FontSize(8);
                        r.RelativeItem().Text(invoice.DeliveryNoteNo ?? "").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Mode/Terms of Payment").FontSize(8);
                        r.RelativeItem().Text("").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Reference No. & Date").FontSize(8);
                        r.RelativeItem().Text(invoice.ReferenceNo ?? "").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Other References").FontSize(8);
                        r.RelativeItem().Text("").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Buyer's Order No.").FontSize(8);
                        r.RelativeItem().Text(invoice.BuyersOrderNo ?? "").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Dispatch Doc No.").FontSize(8);
                        r.RelativeItem().Text(invoice.DispatchDocNo ?? "").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Dispatched through").FontSize(8);
                        r.RelativeItem().Text(invoice.TransportDetails ?? "").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Destination").FontSize(8);
                        r.RelativeItem().Text(invoice.Destination ?? "").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Terms of Delivery").FontSize(8);
                        r.RelativeItem().Text(invoice.TermsOfDelivery ?? "").FontSize(8);
                    });
                });
            });

            // Buyer (Bill To)
            column.Item().Border(1).Padding(5).Column(col =>
            {
                col.Item().Text("Buyer (Bill To)").FontSize(8).Italic();
                col.Item().Text(invoice.Customer.Name).Bold().FontSize(9);
                col.Item().Text(invoice.Customer.BillingAddress).FontSize(8);
                col.Item().Text($"GSTIN/UIN: {invoice.Customer.GSTIN}").FontSize(8);
                col.Item().Text($"State Name: {invoice.Customer.StateName}, Code: {invoice.Customer.StateCode}").FontSize(8);
            });

            // ASN Number (if present)
            if (!string.IsNullOrWhiteSpace(invoice.AsnNo))
            {
                column.Item().PaddingTop(3).Text($"ASN # {invoice.AsnNo}").Bold().FontSize(9);
            }

            // Items Table
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Sl No
                    columns.RelativeColumn(3);   // Description
                    columns.RelativeColumn(1);   // HSN/SAC
                    columns.RelativeColumn(1.2f); // Quantity
                    columns.RelativeColumn(0.8f); // Rate
                    columns.RelativeColumn(0.6f); // Per
                    columns.RelativeColumn(1.2f); // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Sl\nNo").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Description of Goods").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("HSN/SAC").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Quantity").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Rate").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("per").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Amount").Bold().FontSize(8);
                });

                // Item row
                table.Cell().Border(1).Padding(3).AlignCenter().Text("1").FontSize(8);
                table.Cell().Border(1).Padding(3).Column(col =>
                {
                    col.Item().Text($"{invoice.Product.PartNo}").FontSize(8);
                    col.Item().Text(invoice.Product.PartName).FontSize(8);
                });
                table.Cell().Border(1).Padding(3).AlignCenter().Text(invoice.Product.HsnSac).FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text($"{invoice.Quantity:F0} {invoice.Product.Unit}").FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(invoice.Rate.ToString("F2")).FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(invoice.Product.Unit).FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.TaxableValue.ToString("N2")).FontSize(8);

                // Tax rows
                if (invoice.IgstAmount > 0)
                {
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignRight().Text("IGST").FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(invoice.Product.HsnSac).FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.IgstAmount.ToString("N2")).FontSize(8);
                }

                if (invoice.CgstAmount > 0)
                {
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignRight().Text("CGST").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.CgstAmount.ToString("N2")).FontSize(8);
                }

                if (invoice.SgstAmount > 0)
                {
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignRight().Text("SGST").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.SgstAmount.ToString("N2")).FontSize(8);
                }

                // Total row
                table.Cell().ColumnSpan(6).Border(1).Padding(3).AlignRight().Text("Total").Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text($"₹ {invoice.TotalAmount:N2}").Bold().FontSize(9);
            });

            // Amount in words
            column.Item().PaddingTop(3).Border(1).Padding(5).Column(col =>
            {
                col.Item().Text("Amount Chargeable (in words)").FontSize(7).Italic();
                col.Item().Text(invoice.AmountInWords ?? "").Bold().FontSize(9);
            });

            // Tax Summary Table
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);   // HSN/SAC
                    columns.RelativeColumn(1);   // Taxable Value
                    columns.RelativeColumn(0.8f); // Rate
                    columns.RelativeColumn(1);   // Amount
                    columns.RelativeColumn(1);   // Total Tax
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("HSN/SAC").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Taxable Value").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Integrated Tax\nRate").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Integrated Tax\nAmount").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Total Tax\nAmount").Bold().FontSize(7);
                });

                var totalTax = invoice.IgstAmount + invoice.CgstAmount + invoice.SgstAmount;
                table.Cell().Border(1).Padding(3).AlignCenter().Text(invoice.Product.HsnSac).FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.TaxableValue.ToString("N2")).FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text($"{invoice.IgstRate}%").FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.IgstAmount.ToString("N2")).FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(totalTax.ToString("N2")).FontSize(8);

                // Total row
                table.Cell().Border(1).Padding(3).AlignRight().Text("Total").Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.TaxableValue.ToString("N2")).Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).Text("").FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.IgstAmount.ToString("N2")).Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(totalTax.ToString("N2")).Bold().FontSize(8);
            });

            // Tax Amount in Words
            column.Item().PaddingTop(3).Text($"Tax Amount (in words): {invoice.AmountInWords}").FontSize(8).Italic();

            // Bank Details & Declaration
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Declaration").Bold().FontSize(8);
                    col.Item().Text("We declare that this invoice shows the actual price of the").FontSize(7);
                    col.Item().Text("goods described and that all particulars are true and correct.").FontSize(7);
                    col.Item().PaddingTop(20).Text("Customer's Seal and Signature").FontSize(7);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Company's Bank Details").Bold().FontSize(8);
                    col.Item().Text($"A/c Holder's Name: {company["Name"]}").FontSize(7);
                    col.Item().Text($"Bank Name: {company["BankName"]}").FontSize(7);
                    col.Item().Text($"A/c No.: {company["AccountNo"]}").FontSize(7);
                    col.Item().Text($"Branch & IFS Code: {company["BankBranch"]} & {company["BranchIfsc"]}").FontSize(7);
                    col.Item().PaddingTop(20).AlignRight().Text($"for {company["Name"]}").Bold().FontSize(8);
                    col.Item().AlignRight().Text("Authorised Signatory").FontSize(7);
                });
            });
        });
    }
}
