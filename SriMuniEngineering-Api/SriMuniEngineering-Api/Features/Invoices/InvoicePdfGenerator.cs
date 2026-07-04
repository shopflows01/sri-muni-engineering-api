using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SriMuniEngineering_Api.Domain.Entities;

namespace SriMuniEngineering_Api.Features.Invoices;

public static class InvoicePdfGenerator
{
    private static readonly string LogoPath = Path.Combine(
        AppContext.BaseDirectory, "Assets", "sme-logo.png");

    /// <summary>
    /// Generates the invoice PDF that is uploaded to storage.
    /// Single page, no labels.
    /// </summary>
    public static byte[] Generate(Invoice invoice, IConfigurationSection companyProfile)
    {
        return Generate(invoice, companyProfile, false, false, false);
    }

    /// <summary>
    /// Generates the invoice PDF with optional label pages.
    /// If no label is true → single page without label.
    /// Each true label → one additional page with that label.
    /// </summary>
    public static byte[] Generate(
        Invoice invoice,
        IConfigurationSection companyProfile,
        bool originalForRecipient,
        bool duplicateForTransporter,
        bool triplicateForSupplier)
    {
        var labels = new List<string?>();

        if (originalForRecipient)
            labels.Add("ORIGINAL FOR RECIPIENT");
        if (duplicateForTransporter)
            labels.Add("DUPLICATE FOR TRANSPORTER");
        if (triplicateForSupplier)
            labels.Add("TRIPLICATE FOR SUPPLIER");

        // If no labels selected, generate single page without label
        if (labels.Count == 0)
            labels.Add(null);

        var document = Document.Create(container =>
        {
            foreach (var label in labels)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(header => ComposeHeader(header, label));
                    page.Content().Element(content => ComposeContent(content, invoice, companyProfile));
                    page.Footer().AlignCenter().Text("SUBJECT TO HOSUR JURISDICTION").FontSize(8).Bold();
                });
            }
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string? copyLabel)
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
                    if (!string.IsNullOrEmpty(copyLabel))
                    {
                        c.Item().AlignCenter().Text($"({copyLabel})").FontSize(8).Italic();
                    }
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
                    col.Item().Text($"GSTIN/UIN: {company["Gstin"]?.ToUpper()}").FontSize(8);
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
                        r.RelativeItem().Text("E-Way Bill No.").FontSize(8);
                        r.RelativeItem().Text(invoice.EwbNo ?? "").FontSize(8);
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("ASN No.").FontSize(8);
                        r.RelativeItem().Text(invoice.AsnNo ?? "").FontSize(8);
                    });
                });
            });

            // Buyer (Bill To)
            column.Item().Border(1).Padding(5).Column(col =>
            {
                col.Item().Text("Buyer (Bill To)").FontSize(8).Italic();
                col.Item().Text(invoice.Customer.Name).Bold().FontSize(9);
                col.Item().Text(invoice.Customer.BillingAddress).FontSize(8);
                col.Item().Text($"GSTIN/UIN: {invoice.Customer.GSTIN.ToUpper()}").FontSize(8);
                col.Item().Text($"State Name: {invoice.Customer.StateName}, Code: {invoice.Customer.StateCode}").FontSize(8);
            });

            // Items Table with inline GST
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);  // Sl No
                    columns.RelativeColumn(2.5f);// Description
                    columns.RelativeColumn(0.8f);// HSN/SAC
                    columns.RelativeColumn(0.8f);// Quantity
                    columns.RelativeColumn(0.8f);// Rate
                    columns.RelativeColumn(0.6f);// Per
                    columns.RelativeColumn(0.9f);// Taxable Value
                    columns.RelativeColumn(0.9f);// CGST
                    columns.RelativeColumn(0.9f);// SGST
                    columns.RelativeColumn(0.9f);// IGST
                    columns.RelativeColumn(1f);  // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Sl\nNo").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Description of Goods").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("HSN/SAC").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Qty").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Rate").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Per").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Taxable\nValue").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("CGST").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("SGST").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("IGST").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(7);
                });

                int slNo = 1;
                foreach (var item in invoice.Items)
                {
                    bool isInterState = company["StateCode"] != invoice.Customer.StateCode.ToString();
                    
                    decimal cgstPercent = isInterState ? 0 : item.GSTPercent / 2;
                    decimal sgstPercent = isInterState ? 0 : item.GSTPercent / 2;
                    decimal igstPercent = isInterState ? item.GSTPercent : 0;
                    
                    decimal cgstAmt = isInterState ? 0 : item.GSTAmount / 2;
                    decimal sgstAmt = isInterState ? 0 : item.GSTAmount / 2;
                    decimal igstAmt = isInterState ? item.GSTAmount : 0;

                    table.Cell().Border(1).Padding(2).AlignCenter().Text(slNo.ToString()).FontSize(7);
                    table.Cell().Border(1).Padding(2).Column(col =>
                    {
                        col.Item().Text(item.Product.PartNo).FontSize(7);
                        col.Item().Text(item.Product.PartName).FontSize(7);
                        if (!string.IsNullOrEmpty(item.Description))
                            col.Item().Text(item.Description).FontSize(6).Italic();
                    });
                    table.Cell().Border(1).Padding(2).AlignCenter().Text(item.Product.HsnSac).FontSize(7);
                    table.Cell().Border(1).Padding(2).AlignCenter().Text($"{item.Quantity:F0} {item.Product.Unit}").FontSize(7);
                    table.Cell().Border(1).Padding(2).AlignRight().Text(item.Rate.ToString("F2")).FontSize(7);
                    table.Cell().Border(1).Padding(2).AlignCenter().Text(item.Product.Unit).FontSize(7);
                    
                    var taxableValue = (item.Quantity * item.Rate) - item.Discount;
                    table.Cell().Border(1).Padding(2).AlignRight().Text(taxableValue.ToString("N2")).FontSize(7);
                    
                    // CGST
                    table.Cell().Border(1).Padding(2).AlignCenter().Column(col => {
                        if (cgstPercent > 0) {
                            col.Item().Text($"{cgstPercent:F1}%").FontSize(6);
                            col.Item().Text(cgstAmt.ToString("N2")).FontSize(7);
                        } else {
                            col.Item().Text("-").FontSize(7);
                        }
                    });
                    // SGST
                    table.Cell().Border(1).Padding(2).AlignCenter().Column(col => {
                        if (sgstPercent > 0) {
                            col.Item().Text($"{sgstPercent:F1}%").FontSize(6);
                            col.Item().Text(sgstAmt.ToString("N2")).FontSize(7);
                        } else {
                            col.Item().Text("-").FontSize(7);
                        }
                    });
                    // IGST
                    table.Cell().Border(1).Padding(2).AlignCenter().Column(col => {
                        if (igstPercent > 0) {
                            col.Item().Text($"{igstPercent:F1}%").FontSize(6);
                            col.Item().Text(igstAmt.ToString("N2")).FontSize(7);
                        } else {
                            col.Item().Text("-").FontSize(7);
                        }
                    });
                    
                    table.Cell().Border(1).Padding(2).AlignRight().Text(item.Amount.ToString("N2")).FontSize(7);

                    slNo++;
                }

                // Total row
                table.Cell().ColumnSpan(10).Border(1).Padding(2).AlignRight().Text("Total").Bold().FontSize(8);
                table.Cell().Border(1).Padding(2).AlignRight().Text($"₹ {invoice.GrandTotal:N2}").Bold().FontSize(8);
            });

            // Amount in words
            column.Item().PaddingTop(3).Border(1).Padding(5).Column(col =>
            {
                col.Item().Text("Amount Chargeable (in words)").FontSize(7).Italic();
                col.Item().Text(invoice.AmountInWords ?? "").Bold().FontSize(9);
            });

            // Tax Summary Table - Breakdown by GST rate
            var taxGroups = invoice.Items
                .GroupBy(i => new { i.GSTPercent, i.Product.HsnSac })
                .Select(g => new
                {
                    g.Key.HsnSac,
                    g.Key.GSTPercent,
                    TaxableValue = g.Sum(i => (i.Quantity * i.Rate) - i.Discount),
                    TaxAmount = g.Sum(i => i.GSTAmount)
                })
                .OrderBy(g => g.HsnSac)
                .ThenBy(g => g.GSTPercent)
                .ToList();

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);   // HSN/SAC
                    columns.RelativeColumn(1);   // Taxable Value
                    columns.RelativeColumn(1);   // CGST
                    columns.RelativeColumn(1);   // SGST
                    columns.RelativeColumn(1);   // IGST
                    columns.RelativeColumn(1);   // Total Tax Amount
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("HSN/SAC").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Taxable\nValue").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Central Tax\nRate & Amt").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("State Tax\nRate & Amt").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Integrated Tax\nRate & Amt").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Total Tax\nAmount").Bold().FontSize(7);
                });

                bool isInterState = company["StateCode"] != invoice.Customer.StateCode.ToString();
                decimal totalCgst = 0;
                decimal totalSgst = 0;
                decimal totalIgst = 0;

                foreach (var group in taxGroups)
                {
                    decimal cgstPercent = isInterState ? 0 : group.GSTPercent / 2;
                    decimal sgstPercent = isInterState ? 0 : group.GSTPercent / 2;
                    decimal igstPercent = isInterState ? group.GSTPercent : 0;
                    
                    decimal cgstAmt = isInterState ? 0 : group.TaxAmount / 2;
                    decimal sgstAmt = isInterState ? 0 : group.TaxAmount / 2;
                    decimal igstAmt = isInterState ? group.TaxAmount : 0;
                    
                    totalCgst += cgstAmt;
                    totalSgst += sgstAmt;
                    totalIgst += igstAmt;

                    table.Cell().Border(1).Padding(3).AlignCenter().Text(group.HsnSac).FontSize(7);
                    table.Cell().Border(1).Padding(3).AlignRight().Text(group.TaxableValue.ToString("N2")).FontSize(7);
                    
                    table.Cell().Border(1).Padding(3).AlignCenter().Column(col => {
                        if (cgstPercent > 0) {
                            col.Item().Text($"{cgstPercent:F1}%").FontSize(6);
                            col.Item().Text(cgstAmt.ToString("N2")).FontSize(7);
                        } else col.Item().Text("-").FontSize(7);
                    });
                    
                    table.Cell().Border(1).Padding(3).AlignCenter().Column(col => {
                        if (sgstPercent > 0) {
                            col.Item().Text($"{sgstPercent:F1}%").FontSize(6);
                            col.Item().Text(sgstAmt.ToString("N2")).FontSize(7);
                        } else col.Item().Text("-").FontSize(7);
                    });
                    
                    table.Cell().Border(1).Padding(3).AlignCenter().Column(col => {
                        if (igstPercent > 0) {
                            col.Item().Text($"{igstPercent:F1}%").FontSize(6);
                            col.Item().Text(igstAmt.ToString("N2")).FontSize(7);
                        } else col.Item().Text("-").FontSize(7);
                    });
                    
                    table.Cell().Border(1).Padding(3).AlignRight().Text(group.TaxAmount.ToString("N2")).FontSize(7);
                }

                // Total row
                table.Cell().Border(1).Padding(3).AlignRight().Text("Total").Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.SubTotal.ToString("N2")).Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(totalCgst > 0 ? totalCgst.ToString("N2") : "-").Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(totalSgst > 0 ? totalSgst.ToString("N2") : "-").Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(totalIgst > 0 ? totalIgst.ToString("N2") : "-").Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignRight().Text(invoice.GSTAmount.ToString("N2")).Bold().FontSize(8);
            });

            // Tax Amount in Words
            column.Item().PaddingTop(3).Text($"Tax Amount (in words): {invoice.AmountInWords}").FontSize(8).Italic();

            // Bank Details & Declaration
            column.Item().ExtendVertical().AlignBottom().PaddingTop(10).Row(row =>
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
