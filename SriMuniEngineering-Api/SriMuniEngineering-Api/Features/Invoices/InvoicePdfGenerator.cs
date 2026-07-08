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
        return Generate(invoice, companyProfile, false, false, false);
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

        if (labels.Count == 0) labels.Add(null);

        var document = Document.Create(container =>
        {
            foreach (var label in labels)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));

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

                row.RelativeItem().AlignCenter().AlignMiddle().Text("TAX INVOICE").Bold().FontSize(15);

                var rightCol = row.ConstantItem(180).AlignRight().AlignTop();
                if (!string.IsNullOrEmpty(copyLabel))
                {
                    rightCol.Text($"({copyLabel})").FontSize(10).Italic();
                }
            });
            col.Item().PaddingBottom(5).LineHorizontal(1);
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice, IConfigurationSection company)
    {
        container.Column(column =>
        {
            column.Item().Border(1).Row(row =>
            {
                row.RelativeItem().BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Text(company["Name"]!).Bold().FontSize(12);
                    col.Item().Text($"{company["Address1"]}").FontSize(9);
                    col.Item().Text($"{company["Address2"]}").FontSize(9);
                    col.Item().Text($"{company["City"]} - {company["Pincode"]}").FontSize(9);
                    col.Item().Text($"GSTIN/UIN: {company["Gstin"]?.ToUpper()}").FontSize(9);
                    col.Item().Text($"State Name: {company["State"]}, Code: {company["StateCode"]}").FontSize(9);
                    col.Item().Text($"Contact: {company["Phone"]}, {company["AltPhone"]}").FontSize(9);
                    col.Item().Text($"E-Mail: {company["Email"]}").FontSize(9);
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
                            cell.Border(1).Padding(3).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8);
                                c.Item().Text(value).Bold().FontSize(10);
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

            column.Item().Border(1).Padding(5).Column(col =>
            {
                col.Item().Text("Buyer (Bill To)").FontSize(9).Italic();
                col.Item().Text(invoice.Customer.Name).Bold().FontSize(11);
                col.Item().Text(invoice.Customer.BillingAddress).FontSize(9);
                col.Item().Text($"GSTIN/UIN: {invoice.Customer.GSTIN.ToUpper()}").FontSize(9);
                col.Item().Text($"State Name: {invoice.Customer.StateName}, Code: {invoice.Customer.StateCode}").FontSize(9);
            });

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);
                    columns.RelativeColumn(3f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(1f);
                    columns.RelativeColumn(0.8f);
                    columns.RelativeColumn(1.2f);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(4).AlignCenter().Text("Sl\nNo").Bold().FontSize(9);
                    header.Cell().Border(1).Padding(4).AlignCenter().Text("Description of Goods").Bold().FontSize(9);
                    header.Cell().Border(1).Padding(4).AlignCenter().Text("HSN/SAC").Bold().FontSize(9);
                    header.Cell().Border(1).Padding(4).AlignCenter().Text("Quantity").Bold().FontSize(9);
                    header.Cell().Border(1).Padding(4).AlignCenter().Text("Rate").Bold().FontSize(9);
                    header.Cell().Border(1).Padding(4).AlignCenter().Text("per").Bold().FontSize(9);
                    header.Cell().Border(1).Padding(4).AlignCenter().Text("Amount").Bold().FontSize(9);
                });

                int slNo = 1;
                foreach (var item in invoice.Items)
                {
                    table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignCenter().Text(slNo.ToString()).FontSize(9);
                    table.Cell().BorderLeft(1).BorderRight(1).Padding(4).Column(col =>
                    {
                        col.Item().Text(item.Product.PartName).Bold().FontSize(11);
                        if (!string.IsNullOrEmpty(item.Description))
                            col.Item().Text(item.Description).FontSize(9);
                    });
                    table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignCenter().Text(item.HsnCode ?? item.Product.HsnSac).FontSize(9);
                    table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignCenter().Text($"{item.Quantity:F0} {item.Product.Unit}").FontSize(9);
                    table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignRight().Text(item.Rate.ToString("F2")).FontSize(9);
                    table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignCenter().Text(item.Product.Unit).FontSize(9);
                    table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignRight().Text(item.Amount.ToString("N2")).Bold().FontSize(10);

                    slNo++;
                }

                bool isInterState = company["StateCode"] != invoice.Customer.StateCode.ToString();
                decimal totalCgst = isInterState ? 0 : invoice.GSTAmount / 2;
                decimal totalSgst = isInterState ? 0 : invoice.GSTAmount / 2;
                decimal totalIgst = isInterState ? invoice.GSTAmount : 0;

                void AddTaxRow(string name, decimal amt) {
                    if (amt > 0) {
                        table.Cell().BorderLeft(1).BorderRight(1).Padding(4).Text("");
                        table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignRight().Text(name).Bold().Italic().FontSize(10);
                        table.Cell().BorderLeft(1).BorderRight(1).Padding(4).Text("");
                        table.Cell().BorderLeft(1).BorderRight(1).Padding(4).Text("");
                        table.Cell().BorderLeft(1).BorderRight(1).Padding(4).Text("");
                        table.Cell().BorderLeft(1).BorderRight(1).Padding(4).Text("");
                        table.Cell().BorderLeft(1).BorderRight(1).Padding(4).AlignRight().Text(amt.ToString("N2")).Bold().FontSize(10);
                    }
                }

                AddTaxRow("CGST", totalCgst);
                AddTaxRow("SGST", totalSgst);
                AddTaxRow("IGST", totalIgst);

                for (uint col = 1; col <= 7; col++)
                {
                    table.Cell().Column(col).BorderLeft(1).BorderRight(1).MinHeight(150);
                }

                table.Cell().ColumnSpan(6).Border(1).Padding(4).AlignRight().Text("Total").Bold().FontSize(11);
                table.Cell().Border(1).Padding(4).AlignRight().Text($"₹ {invoice.GrandTotal:N2}").Bold().FontSize(11);
            });

            column.Item().PaddingTop(3).Border(1).Padding(5).Column(col =>
            {
                col.Item().Text("Amount Chargeable (in words)").FontSize(9).Italic();
                col.Item().Text(invoice.AmountInWords ?? "").Bold().FontSize(12);
            });

            var taxWords = ConvertToWords(invoice.GSTAmount);
            column.Item().PaddingTop(5).Text(text => 
            {
                 text.Span("Tax Amount (in words) : ").FontSize(9).Italic();
                 text.Span(taxWords).Bold().FontSize(11);
            });

            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Declaration").Bold().FontSize(9);
                    col.Item().Text("We declare that this invoice shows the actual price of the").FontSize(8);
                    col.Item().Text("goods described and that all particulars are true and correct.").FontSize(8);
                    col.Item().PaddingTop(20).Text("Customer's Seal and Signature").FontSize(8);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Company's Bank Details").Bold().FontSize(9);
                    col.Item().Text($"A/c Holder's Name : {company["Name"]}").FontSize(8);
                    col.Item().Text($"Bank Name : {company["BankName"]}").FontSize(8);
                    col.Item().Text($"A/c No. : {company["AccountNo"]}").FontSize(8);
                    col.Item().Text($"Branch & IFS Code : {company["BankBranch"]} & {company["BranchIfsc"]}").FontSize(8);
                    col.Item().PaddingTop(20).AlignRight().Text($"for {company["Name"]}").Bold().FontSize(9);
                    col.Item().AlignRight().Text("Authorised Signatory").FontSize(8);
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
