using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SriMuniEngineering_Api.Domain.Entities;

namespace SriMuniEngineering_Api.Features.DeliveryChallans;

public static class DeliveryChallanPdfGenerator
{
    private static readonly string LogoPath = Path.Combine(
        AppContext.BaseDirectory, "Assets", "svi-logo.png");

    public static byte[] Generate(DeliveryChallan dc, IConfigurationSection companyProfile)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Element(content => ComposeContent(content, dc, companyProfile));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeContent(IContainer container, DeliveryChallan dc, IConfigurationSection company)
    {
        container.Border(1).Column(column =>
        {
            // Header Section
            column.Item().BorderBottom(1).Padding(3).Row(row =>
            {
                var leftCol = row.ConstantItem(100).AlignLeft().AlignMiddle();
                if (File.Exists(LogoPath))
                {
                    leftCol.Width(80).Image(LogoPath);
                }

                row.RelativeItem().AlignCenter().Column(col =>
                {
                    col.Item().AlignCenter().Text("DELIVERY CHALLAN").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text(company["Name"]).Bold().FontSize(16).FontColor(Colors.Blue.Darken4);
                    col.Item().AlignCenter().Text($"{company["Address1"]}, {company["Address2"]}, {company["City"]} - {company["Pincode"]}. E-mail : {company["Email"]}").FontSize(8).FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text($"Cell : {company["Phone"]}, {company["AltPhone"]}").Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                    col.Item().AlignCenter().Text($"GSTIN : {company["Gstin"]}").Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                });

                row.ConstantItem(100); // Empty right column to perfectly center the middle content
            });

            // Customer and Document Details Section
            column.Item().BorderBottom(1).Row(row =>
            {
                // Left Side (Customer)
                row.RelativeItem().Padding(3).Column(col =>
                {
                    col.Item().Text("To").FontSize(9);
                    col.Item().Text($"M/s. {dc.Customer.Name}").Bold().FontSize(11);
                    col.Item().Text(dc.Customer.BillingAddress).FontSize(9);
                });

                // Right Side (Document Info)
                row.RelativeItem().BorderLeft(1).Column(col =>
                {
                    col.Item().BorderBottom(1).Padding(3).Row(r => {
                        r.ConstantItem(55).Text("D.C No.:").FontSize(9);
                        r.RelativeItem().AlignCenter().Text(dc.DcNo).Bold().FontSize(11).FontColor(Colors.Red.Medium);
                        r.AutoItem().Text($"Date: {dc.DcDate:dd/MM/yyyy}").FontSize(9);
                    });
                    col.Item().BorderBottom(1).Padding(3).Row(r => {
                        r.RelativeItem().Text($"Your D.C. No.    {dc.YourDcNo ?? ""}").FontSize(9);
                        r.AutoItem().Text($"Date : {dc.YourDcDate?.ToString("dd/MM/yyyy") ?? ""}").FontSize(9);
                    });
                    col.Item().BorderBottom(1).Padding(3).Text($"P.O.No. Cide No.    {dc.PoNo ?? ""}").FontSize(9);
                    col.Item().Padding(3).Text($"GST No.    {dc.Customer.GSTIN}").FontSize(9);
                });
            });

            // Table Header
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.RelativeColumn();
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(100);
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).BorderRight(1).Padding(3).AlignCenter().Text("Sl.\nNo.").Bold().FontSize(9);
                    header.Cell().BorderBottom(1).BorderRight(1).Padding(3).AlignCenter().Text("DESCRIPTION").Bold().FontSize(9);
                    header.Cell().BorderBottom(1).BorderRight(1).Padding(3).AlignCenter().Text("Qty").Bold().FontSize(9);
                    header.Cell().BorderBottom(1).Padding(3).AlignCenter().Text("Remarks").Bold().FontSize(9);
                });

                // Table Items
                int slNo = 1;
                foreach (var item in dc.Items)
                {
                    table.Cell().BorderRight(1).Padding(3).AlignCenter().Text(slNo.ToString()).FontSize(9);
                    table.Cell().BorderRight(1).Padding(3).Text($"{item.Product.PartName}").FontSize(10);
                    table.Cell().BorderRight(1).Padding(3).Column(c => {
                        c.Item().AlignCenter().Text(item.Quantity.ToString()).FontSize(10);
                        c.Item().PaddingHorizontal(8).LineHorizontal(1);
                        c.Item().AlignCenter().Text(string.IsNullOrWhiteSpace(item.Unit) ? item.Product.Unit : item.Unit).FontSize(9);
                    });
                    table.Cell().Padding(3).AlignCenter().Text(item.Remarks ?? "").FontSize(9);

                    slNo++;
                }

                // Fill remaining empty space dynamically
                table.Cell().BorderRight(1).ExtendVertical().Text("");
                table.Cell().BorderRight(1).ExtendVertical().Text("");
                table.Cell().BorderRight(1).ExtendVertical().Text("");
                table.Cell().ExtendVertical().Text("");

                // Footer inside table
                table.Cell().ColumnSpan(4).BorderTop(1).Padding(3).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Received the above goods in good order & Condition.").FontSize(8);
                        row.RelativeItem().AlignRight().Text($"For {company["Name"]}").Bold().FontSize(9);
                    });

                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Text("Receiver's Signature").FontSize(9);
                        row.RelativeItem().AlignRight().Text("Authorised Signatory").FontSize(9);
                    });
                });
            });
        });
    }
}
