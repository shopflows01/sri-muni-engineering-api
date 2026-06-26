using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.Quotations.Dtos;

namespace SriMuniEngineering_Api.Features.Quotations;

public static class QuotationPdfGenerator
{
    private static readonly string LogoPath = Path.Combine(
        AppContext.BaseDirectory, "Assets", "sme-logo.png");

    public static byte[] Generate(Quotation quotation, IConfigurationSection companyProfile)
    {
        var operations = JsonSerializer.Deserialize<List<OperationItem>>(quotation.OperationsJson) ?? [];
        var otherCosts = JsonSerializer.Deserialize<OtherCosts>(quotation.OtherCostsJson) ?? new();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => ComposeHeader(header, companyProfile));
                page.Content().Element(content => ComposeContent(content, quotation, operations, otherCosts, companyProfile));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, IConfigurationSection company)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                // Logo on the left
                row.ConstantItem(70).AlignMiddle().Column(col =>
                {
                    if (File.Exists(LogoPath))
                    {
                        col.Item().Width(60).Image(LogoPath);
                    }
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"GSTIN : {company["Gstin"]}").FontSize(8);
                    col.Item().Text($"PAN : {company["Pan"]}").FontSize(8);
                });

                row.RelativeItem(2).AlignCenter().Column(col =>
                {
                    col.Item().AlignCenter().Text(company["Name"]!).Bold().FontSize(14);
                    col.Item().AlignCenter().Text(company["TagLine"]!).FontSize(7).Italic();
                    col.Item().AlignCenter().Text($"{company["Address1"]}, {company["Address2"]}").FontSize(8);
                    col.Item().AlignCenter().Text($"SIPCOT, {company["City"]!.ToUpper()} - {company["Pincode"]}").FontSize(8);
                    col.Item().AlignCenter().Text($"Email: {company["Email"]}").FontSize(8);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text($"Mobile: {company["Phone"]}").FontSize(8);
                    col.Item().AlignRight().Text(company["AltPhone"]!).FontSize(8);
                });
            });

            column.Item().PaddingVertical(5).LineHorizontal(1);
        });
    }

    private static void ComposeContent(IContainer container, Quotation quotation, List<OperationItem> operations, OtherCosts otherCosts, IConfigurationSection company)
    {
        container.Column(column =>
        {
            // Title
            column.Item().PaddingVertical(5).Row(row =>
            {
                row.RelativeItem(2).Border(1).Padding(5).Column(col =>
                {
                    col.Item().AlignCenter().Text(company["Name"]!).Bold().FontSize(11);
                    col.Item().AlignCenter().Text($"{company["Address1"]}, {company["Address2"]}").FontSize(8);
                    col.Item().AlignCenter().Text($"SIPCOT, {company["City"]!.ToUpper()} - {company["Pincode"]}").FontSize(8);
                    col.Item().AlignCenter().Text($"V CODE {quotation.Customer.VendorCode ?? ""}").FontSize(8);
                });
                row.RelativeItem().Border(1).Padding(5).Column(col =>
                {
                    col.Item().Text($"Date: {quotation.Date:dd-M-yyyy}").FontSize(9);
                });
            });

            column.Item().PaddingVertical(3).AlignCenter().Text("COMPONENT COSTING SHEET").Bold().FontSize(11);

            // Part Details
            column.Item().Border(1).Padding(5).Column(col =>
            {
                col.Item().Text("PART DETAILS").Bold().FontSize(9);
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text($"Part No. / Issue: {quotation.Product.PartNo}").FontSize(9);
                    row.RelativeItem().Text($"Model: {quotation.Model ?? ""}").FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Part Description: {quotation.Product.PartDescription ?? quotation.Product.PartName}").FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"No. off: {quotation.NumberOff}").FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Customer Name / Code: M/s {quotation.Customer.Name}").FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Location & State: {quotation.Customer.ShippingAddress}").FontSize(9);
                });
            });

            // Part Process Details Table
            column.Item().PaddingTop(10).Text("PART PROCESS DETAILS").Bold().FontSize(9);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);  // Opn No
                    columns.RelativeColumn(3);   // Sequence of Operation
                    columns.RelativeColumn(1);   // Machine
                    columns.RelativeColumn(1);   // Output/Hour
                    columns.RelativeColumn(1.2f); // Machine Hour Rate
                    columns.RelativeColumn(1);   // Cost/Part
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Opn No.").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Sequence of Operation").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Machine").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Output/ Hour").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Machine Hour Rate").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Cost / Part").Bold().FontSize(8);
                });

                foreach (var op in operations)
                {
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(op.OpnNo.ToString()).FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(op.SequenceOfOperation).FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(op.Machine).FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(op.OutputPerHour.ToString()).FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(op.MachineHourRate.ToString("F2")).FontSize(8);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(op.CostPerPart.ToString("F2")).FontSize(8);
                }

                // Total row
                table.Cell().ColumnSpan(5).Border(1).Padding(3).AlignRight().Text("TOTAL #").Bold().FontSize(8);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(quotation.ProcessCostTotal.ToString("F2")).Bold().FontSize(8);
            });

            // Other Cost Details
            column.Item().PaddingTop(10).Text("OTHER COST DETAILS").Bold().FontSize(9);
            column.Item().Border(1).Padding(5).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Text("Process Cost").FontSize(9);
                    row.RelativeItem().AlignRight().Text(quotation.ProcessCostTotal.ToString("F2")).FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Text("Tools cost").FontSize(9);
                    row.RelativeItem().AlignRight().Text(otherCosts.ToolsCost.ToString("F2")).FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Text("Inspection").FontSize(9);
                    row.RelativeItem().AlignRight().Text(otherCosts.InspectionCost.ToString("F2")).FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Text("Oiling & packing").FontSize(9);
                    row.RelativeItem().AlignRight().Text(otherCosts.OilingPackingCost.ToString("F2")).FontSize(9);
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem(3).Text("Others (Eb, machine mainten, gloues, rent, ...etc..)").FontSize(9);
                    row.RelativeItem().AlignRight().Text(otherCosts.OthersCost.ToString("F2")).FontSize(9);
                });
            });

            // Estimated Cost
            column.Item().PaddingTop(10).Border(1).Padding(8).Row(row =>
            {
                row.RelativeItem(3).Text("Estimated Cost per Part").Bold().FontSize(10);
                row.RelativeItem().AlignRight().Text(quotation.EstimatedCostPerPart.ToString("F2")).Bold().FontSize(12);
                row.RelativeItem().AlignRight().Text($"AD {quotation.GstRate}% GST").FontSize(9);
            });
        });
    }
}
