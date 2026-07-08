using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SriMuniEngineering_Api.Domain.Entities;

namespace SriMuniEngineering_Api.Features.Invoices;

public static class InvoicePdfGenerator
{
    private static readonly string LogoPath = Path.Combine(
        AppContext.BaseDirectory, "Assets", "svi-logo.png");

    public static byte[] Generate(Invoice invoice, IConfigurationSection companyProfile)
    {
        return Generate(invoice, companyProfile, true, false, false);
    }

    public static byte[] Generate(
        Invoice invoice,
        IConfigurationSection companyProfile,
        bool originalForRecipient,
        bool duplicateForTransporter,
        bool triplicateForSupplier)
    {
        var labels = new List<string?>();

        if (originalForRecipient) labels.Add("ORIGINAL FOR RECIPIENT");
        if (duplicateForTransporter) labels.Add("DUPLICATE FOR TRANSPORTER");
        if (triplicateForSupplier) labels.Add("TRIPLICATE FOR SUPPLIER");

        if (labels.Count == 0) labels.Add("ORIGINAL FOR RECIPIENT");

        var document = Document.Create(container =>
        {
            foreach (var label in labels)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(header => ComposeHeader(header, label));
                    page.Content().Element(content => ComposeContent(content, invoice, companyProfile));
                    page.Footer().AlignCenter().Text("SUBJECT TO HOSUR JURISDICTION").FontSize(9).Bold();
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
                var leftCol = row.ConstantItem(120).AlignLeft().AlignMiddle();
                if (File.Exists(LogoPath))
                {
                    leftCol.Width(60).Image(LogoPath);
                }

                row.RelativeItem().AlignCenter().AlignMiddle().Text("TAX INVOICE").Bold().FontSize(14);

                var rightCol = row.ConstantItem(180).AlignRight().AlignTop();
                if (!string.IsNullOrEmpty(copyLabel))
                {
                    rightCol.Text($"({copyLabel})").FontSize(10).Italic();
                }
            });
            col.Item().PaddingBottom(3).LineHorizontal(0.5f);
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice, IConfigurationSection company)
    {
        container.Column(column =>
        {
            column.Item().Border(0.5f).Row(row =>
            {
                row.RelativeItem().BorderRight(0.5f).Padding(3).Column(col =>
                {
                    col.Spacing(2);
                    col.Item().Text(company["Name"]!).Bold().FontSize(12);
                    col.Item().Text($"{company["Address1"]}").FontSize(9);
                    col.Item().Text($"{company["Address2"]}").FontSize(9);
                    col.Item().Text($"{company["City"]} - {company["Pincode"]}").FontSize(9);
                    col.Item().Text(t => {
                        t.Span("GSTIN/UIN : ").FontSize(9);
                        t.Span(company["Gstin"]?.ToUpper()).Bold().FontSize(9);
                    });
                    col.Item().Text($"State Name : {company["State"]}, Code : {company["StateCode"]}").FontSize(9);
                    col.Item().Text($"Contact : {company["Phone"]}, {company["AltPhone"]}").FontSize(9);
                    col.Item().Text($"E-Mail : {company["Email"]}").FontSize(9);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        void AddCell(string label, string value, bool fullWidth = false)
                        {
                            var cell = fullWidth ? t.Cell().ColumnSpan(2) : t.Cell();
                            cell.BorderBottom(0.5f).BorderRight(fullWidth ? 0 : 0.5f).Padding(2).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8);
                                c.Item().Text(value).Bold().FontSize(9);
                            });
                        }

                        AddCell("Invoice No.", invoice.InvoiceNo);
                        AddCell("Dated", invoice.Date.ToString("dd-MMM-yyyy"));
                        AddCell("DC No.", invoice.DeliveryNoteNo ?? "");
                        AddCell("DC Date", invoice.DcDate?.ToString("dd-MMM-yyyy") ?? "");
                        AddCell("Buyer's Order No.", invoice.BuyersOrderNo ?? "");
                        AddCell("Buyer's Order Date", "");
                        AddCell("Dispatched through", invoice.DispatchDocNo ?? "");
                        AddCell("Destination", invoice.Destination ?? "");
                        AddCell("Terms of Delivery", invoice.TermsOfDelivery ?? "", true);
                        AddCell("ASN No.", invoice.AsnNo ?? "");
                        AddCell("EWB No.", invoice.EwbNo ?? "");
                    });
                });
            });

            column.Item().Border(0.5f).BorderTop(0).Padding(3).Column(col =>
            {
                col.Spacing(2);
                col.Item().Text("Buyer (Bill To)").FontSize(9).Italic();
                col.Item().Text(invoice.Customer.Name).Bold().FontSize(11);
                col.Item().Text(invoice.Customer.BillingAddress).FontSize(9);
                col.Item().Text(t => {
                    t.Span("GSTIN/UIN : ").FontSize(9);
                    t.Span(invoice.Customer.GSTIN.ToUpper()).Bold().FontSize(9);
                });
                col.Item().Text($"State Name : {invoice.Customer.StateName}, Code : {invoice.Customer.StateCode}").FontSize(9);
            });

            column.Item().PaddingTop(3).Border(0.5f).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(3).AlignCenter().Text("Sl\nNo").Bold().FontSize(9);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(3).AlignCenter().Text("Description of Goods").Bold().FontSize(9);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(3).AlignCenter().Text("HSN/SAC").Bold().FontSize(9);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(3).AlignCenter().Text("Quantity").Bold().FontSize(9);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(3).AlignCenter().Text("Rate").Bold().FontSize(9);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(3).AlignCenter().Text("per").Bold().FontSize(9);
                    header.Cell().BorderBottom(0.5f).Padding(3).AlignCenter().Text("Amount").Bold().FontSize(9);
                });

                int slNo = 1;
                foreach (var item in invoice.Items)
                {
                    table.Cell().BorderRight(0.5f).Padding(3).AlignCenter().Text(slNo.ToString()).FontSize(9);
                    table.Cell().BorderRight(0.5f).Padding(3).Column(col =>
                    {
                        col.Item().Text(item.Product.PartName).Bold().FontSize(11);
                        if (!string.IsNullOrEmpty(item.Description))
                            col.Item().Text(item.Description).FontSize(9);
                    });
                    table.Cell().BorderRight(0.5f).Padding(3).AlignCenter().Text(item.HsnCode ?? item.Product.HsnSac).FontSize(9);
                    table.Cell().BorderRight(0.5f).Padding(3).AlignCenter().Text($"{item.Quantity:F0} {item.Product.Unit}").FontSize(9);
                    table.Cell().BorderRight(0.5f).Padding(3).AlignRight().Text(item.Rate.ToString("F2")).FontSize(9);
                    table.Cell().BorderRight(0.5f).Padding(3).AlignCenter().Text(item.Product.Unit).FontSize(9);
                    table.Cell().Padding(3).AlignRight().Text(item.Amount.ToString("N2")).Bold().FontSize(10);

                    slNo++;
                }

                bool isInterState = company["StateCode"] != invoice.Customer.StateCode.ToString();
                decimal totalCgst = isInterState ? 0 : invoice.GSTAmount / 2;
                decimal totalSgst = isInterState ? 0 : invoice.GSTAmount / 2;
                decimal totalIgst = isInterState ? invoice.GSTAmount : 0;

                void AddTaxRow(string name, decimal amt) {
                    if (amt > 0) {
                        table.Cell().BorderRight(0.5f).Padding(3).Text("");
                        table.Cell().BorderRight(0.5f).Padding(3).AlignRight().Text(name).Bold().Italic().FontSize(10);
                        table.Cell().BorderRight(0.5f).Padding(3).Text("");
                        table.Cell().BorderRight(0.5f).Padding(3).Text("");
                        table.Cell().BorderRight(0.5f).Padding(3).Text("");
                        table.Cell().BorderRight(0.5f).Padding(3).Text("");
                        table.Cell().Padding(3).AlignRight().Text(amt.ToString("N2")).Bold().FontSize(10);
                    }
                }

                AddTaxRow("CGST", totalCgst);
                AddTaxRow("SGST", totalSgst);
                AddTaxRow("IGST", totalIgst);

                for (uint col = 1; col <= 7; col++)
                {
                    table.Cell().Column(col).BorderRight(col == 7 ? 0 : 0.5f).MinHeight(80);
                }

                table.Cell().ColumnSpan(6).BorderTop(0.5f).BorderRight(0.5f).Padding(3).AlignRight().Text("Total").Bold().FontSize(11);
                table.Cell().BorderTop(0.5f).Padding(3).AlignRight().Text($"₹ {invoice.GrandTotal:N2}").Bold().FontSize(11);
            });

            column.Item().PaddingTop(3).Border(0.5f).Padding(5).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Amount Chargeable (in words)").FontSize(9).Italic();
                    col.Item().Text(invoice.AmountInWords ?? "").Bold().FontSize(11);
                });
                row.AutoItem().AlignRight().Text("E. & O.E").Bold().FontSize(10);
            });

            var taxGroups = invoice.Items
                .GroupBy(i => new { i.GSTPercent, HsnSac = i.HsnCode ?? i.Product.HsnSac })
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

            column.Item().PaddingTop(3).Border(0.5f).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text("HSN/SAC").Bold().FontSize(8);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text("Taxable\nValue").Bold().FontSize(8);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text("Central Tax\nRate & Amt").Bold().FontSize(8);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text("State Tax\nRate & Amt").Bold().FontSize(8);
                    header.Cell().BorderBottom(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text("Integrated Tax\nRate & Amt").Bold().FontSize(8);
                    header.Cell().BorderBottom(0.5f).Padding(2).AlignCenter().Text("Total Tax\nAmount").Bold().FontSize(8);
                });

                bool isInterState = company["StateCode"] != invoice.Customer.StateCode.ToString();
                decimal totalCgst = 0, totalSgst = 0, totalIgst = 0;

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

                    table.Cell().BorderRight(0.5f).Padding(2).AlignCenter().Text(group.HsnSac).FontSize(8);
                    table.Cell().BorderRight(0.5f).Padding(2).AlignRight().Text(group.TaxableValue.ToString("N2")).FontSize(8);
                    
                    table.Cell().BorderRight(0.5f).Padding(2).AlignCenter().Column(col => {
                        if (cgstPercent > 0) { col.Item().Text($"{cgstPercent:F1}%").FontSize(7); col.Item().Text(cgstAmt.ToString("N2")).FontSize(8); }
                        else col.Item().Text("-").FontSize(8);
                    });
                    
                    table.Cell().BorderRight(0.5f).Padding(2).AlignCenter().Column(col => {
                        if (sgstPercent > 0) { col.Item().Text($"{sgstPercent:F1}%").FontSize(7); col.Item().Text(sgstAmt.ToString("N2")).FontSize(8); }
                        else col.Item().Text("-").FontSize(8);
                    });
                    
                    table.Cell().BorderRight(0.5f).Padding(2).AlignCenter().Column(col => {
                        if (igstPercent > 0) { col.Item().Text($"{igstPercent:F1}%").FontSize(7); col.Item().Text(igstAmt.ToString("N2")).FontSize(8); }
                        else col.Item().Text("-").FontSize(8);
                    });
                    
                    table.Cell().Padding(2).AlignRight().Text(group.TaxAmount.ToString("N2")).FontSize(8);
                }

                table.Cell().BorderTop(0.5f).BorderRight(0.5f).Padding(2).AlignRight().Text("Total").Bold().FontSize(9);
                table.Cell().BorderTop(0.5f).BorderRight(0.5f).Padding(2).AlignRight().Text(invoice.SubTotal.ToString("N2")).Bold().FontSize(9);
                table.Cell().BorderTop(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text(totalCgst > 0 ? totalCgst.ToString("N2") : "-").Bold().FontSize(9);
                table.Cell().BorderTop(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text(totalSgst > 0 ? totalSgst.ToString("N2") : "-").Bold().FontSize(9);
                table.Cell().BorderTop(0.5f).BorderRight(0.5f).Padding(2).AlignCenter().Text(totalIgst > 0 ? totalIgst.ToString("N2") : "-").Bold().FontSize(9);
                table.Cell().BorderTop(0.5f).Padding(2).AlignRight().Text(invoice.GSTAmount.ToString("N2")).Bold().FontSize(9);
            });

            var taxWords = ConvertToWords(invoice.GSTAmount);
            column.Item().PaddingTop(3).Text(text => 
            {
                 text.Span("Tax Amount (in words) : ").FontSize(9).Italic();
                 text.Span(taxWords).Bold().FontSize(10);
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Declaration").Bold().FontSize(10);
                    col.Item().Text("We declare that this invoice shows the actual price of the").FontSize(9);
                    col.Item().Text("goods described and that all particulars are true and correct.").FontSize(9);
                });

                row.RelativeItem().PaddingLeft(35).Column(col =>
                {
                    col.Item().Text("Company's Bank Details").Bold().FontSize(10);
                    col.Item().Text($"A/c Holder's Name : {company["Name"]}").FontSize(9);
                    col.Item().Text($"Bank Name : {company["BankName"]}").FontSize(9);
                    col.Item().Text($"A/c No. : {company["AccountNo"]}").FontSize(9);
                    col.Item().Text($"Branch & IFS Code : {company["BankBranch"]} & {company["BranchIfsc"]}").FontSize(9);
                });
            });

            column.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Text("Customer's Seal and Signature").FontSize(9);

                row.RelativeItem().PaddingLeft(35).Column(col =>
                {
                    col.Item().AlignRight().Text($"for {company["Name"]}").Bold().FontSize(10);
                    col.Item().PaddingTop(40).AlignRight().Text("Authorised Signatory").FontSize(9);
                });
            });
        });
    }

    private static string ConvertToWords(decimal amount)
    {
        var intPart = (long)Math.Floor(amount);
        var ones = new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        if (intPart == 0) return "Zero Only";

        string words = "";

        if (intPart / 10000000 > 0)
        {
            words += ConvertHundreds((int)(intPart / 10000000), ones, tens) + " Crore ";
            intPart %= 10000000;
        }

        if (intPart / 100000 > 0)
        {
            words += ConvertHundreds((int)(intPart / 100000), ones, tens) + " Lakh ";
            intPart %= 100000;
        }

        if (intPart / 1000 > 0)
        {
            words += ConvertHundreds((int)(intPart / 1000), ones, tens) + " Thousand ";
            intPart %= 1000;
        }

        if (intPart / 100 > 0)
        {
            words += ConvertHundreds((int)intPart, ones, tens);
        }
        else if (intPart > 0)
        {
            words += ConvertTwoDigits((int)intPart, ones, tens);
        }

        return $"INR {words.Trim()} Only";
    }

    private static string ConvertHundreds(int number, string[] ones, string[] tens)
    {
        var result = "";
        if (number / 100 > 0)
        {
            result += ones[number / 100] + " Hundred ";
            number %= 100;
        }
        result += ConvertTwoDigits(number, ones, tens);
        return result.Trim();
    }

    private static string ConvertTwoDigits(int number, string[] ones, string[] tens)
    {
        if (number < 20) return ones[number];
        return (tens[number / 10] + " " + ones[number % 10]).Trim();
    }
}
