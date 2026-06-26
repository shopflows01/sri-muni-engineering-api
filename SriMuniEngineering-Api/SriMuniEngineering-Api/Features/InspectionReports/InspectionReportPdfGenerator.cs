using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.InspectionReports.Dtos;

namespace SriMuniEngineering_Api.Features.InspectionReports;

public static class InspectionReportPdfGenerator
{
    private static readonly string LogoPath = Path.Combine(
        AppContext.BaseDirectory, "Assets", "sme-logo.png");

    public static byte[] Generate(InspectionReport report, IConfigurationSection companyProfile)
    {
        var parameters = JsonSerializer.Deserialize<List<InspectionParameter>>(report.ParametersJson) ?? [];

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content().Element(content => ComposeContent(content, report, parameters, companyProfile));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeContent(IContainer container, InspectionReport report, List<InspectionParameter> parameters, IConfigurationSection company)
    {
        container.Column(column =>
        {
            // Header row with logo
            column.Item().Border(1).Row(row =>
            {
                row.RelativeItem().BorderRight(1).Padding(3).Column(col =>
                {
                    col.Item().Text("Sl. No.").FontSize(7);
                    col.Item().PaddingTop(3).Text("Vendor Name").FontSize(7);
                });
                row.RelativeItem(4).BorderRight(1).Padding(3).AlignCenter().Column(col =>
                {
                    col.Item().Text("PRE - DISPATCH INSPECTION REPORT").Bold().FontSize(11);
                    col.Item().PaddingTop(5).Row(logoRow =>
                    {
                        if (File.Exists(LogoPath))
                        {
                            logoRow.ConstantItem(40).AlignMiddle().Image(LogoPath);
                        }
                        logoRow.RelativeItem().AlignMiddle().PaddingLeft(5)
                            .Text(company["Name"]!).Bold().FontSize(10);
                    });
                });
                row.RelativeItem().Padding(3).Column(col =>
                {
                    col.Item().Text($"Vendor code").FontSize(7);
                    col.Item().Text(report.Customer.VendorCode ?? "").Bold().FontSize(9);
                });
            });

            // Details section
            column.Item().Border(1).Row(row =>
            {
                // Left side
                row.RelativeItem(3).BorderRight(1).Padding(3).Column(col =>
                {
                    col.Item().Text($"Customer Name: {report.Customer.Name}").FontSize(8);
                    col.Item().Text($"Part No.: {report.Product.PartNo}").FontSize(8);
                    col.Item().Text($"Part Name: {report.Product.PartName}").FontSize(8);
                    col.Item().Text($"Operation: {report.Operation}").FontSize(8);
                    col.Item().Text($"MCIE Drawing No.: {report.DrawingNo ?? ""}").FontSize(8);
                });

                // Right side
                row.RelativeItem(2).Padding(3).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"D.C. No.: {report.DcNo}").FontSize(8);
                    });
                    col.Item().Text($"D.C. Date: {report.DcDate:dd/MM/yyyy}").FontSize(8);
                    col.Item().Text($"D.C. Qty.: {report.DcQty}").FontSize(8);
                    col.Item().Text($"Inspected Qty.: {report.InspectedQty}").FontSize(8);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Issue No.: {report.IssueNo ?? ""}").FontSize(8);
                        r.RelativeItem().Text($"Batch No.: {report.BatchNo ?? ""}").FontSize(8);
                    });
                });
            });

            // Parameters Table
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Sl No
                    columns.RelativeColumn(2);   // Parameter
                    columns.RelativeColumn(1.5f); // Drawing Specification
                    columns.RelativeColumn(1);   // Measurement Technics
                    columns.ConstantColumn(40);  // Actual 1
                    columns.ConstantColumn(40);  // Actual 2
                    columns.ConstantColumn(40);  // Actual 3
                    columns.ConstantColumn(40);  // Actual 4
                    columns.ConstantColumn(40);  // Actual 5
                    columns.RelativeColumn(1);   // Remarks
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Sl.\nNo.").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Parameter").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Drawing\nSpecification").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Measure\nment Technics").Bold().FontSize(7);
                    header.Cell().ColumnSpan(5).Border(1).Padding(2).AlignCenter().Text("Actual Dimension").Bold().FontSize(7);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Remarks").Bold().FontSize(7);
                });

                // Sub-header for actual dimensions
                table.Cell().Border(1).Padding(2).Text("").FontSize(7);
                table.Cell().Border(1).Padding(2).Text("").FontSize(7);
                table.Cell().Border(1).Padding(2).Text("").FontSize(7);
                table.Cell().Border(1).Padding(2).Text("").FontSize(7);
                table.Cell().Border(1).Padding(2).AlignCenter().Text("1").Bold().FontSize(7);
                table.Cell().Border(1).Padding(2).AlignCenter().Text("2").Bold().FontSize(7);
                table.Cell().Border(1).Padding(2).AlignCenter().Text("3").Bold().FontSize(7);
                table.Cell().Border(1).Padding(2).AlignCenter().Text("4").Bold().FontSize(7);
                table.Cell().Border(1).Padding(2).AlignCenter().Text("5").Bold().FontSize(7);
                table.Cell().Border(1).Padding(2).Text("").FontSize(7);

                // Data rows
                foreach (var param in parameters)
                {
                    table.Cell().Border(1).Padding(2).AlignCenter().Text(param.SlNo.ToString()).FontSize(7);
                    table.Cell().Border(1).Padding(2).Text(param.Parameter).FontSize(7);
                    table.Cell().Border(1).Padding(2).Text(param.DrawingSpecification).FontSize(7);
                    table.Cell().Border(1).Padding(2).Text(param.MeasurementTechnics).FontSize(7);

                    for (int i = 0; i < 5; i++)
                    {
                        var val = i < param.ActualDimensions.Count ? param.ActualDimensions[i] : "";
                        table.Cell().Border(1).Padding(2).AlignCenter().Text(val).FontSize(7);
                    }

                    table.Cell().Border(1).Padding(2).Text(param.Remarks ?? "").FontSize(7);
                }
            });

            // Footer - Qty results
            column.Item().PaddingTop(10).Border(1).Row(row =>
            {
                row.RelativeItem().BorderRight(1).Padding(3).AlignCenter().Column(col =>
                {
                    col.Item().Text("OK QTY.").Bold().FontSize(8);
                    col.Item().Text(report.OkQty.ToString()).FontSize(9);
                });
                row.RelativeItem().BorderRight(1).Padding(3).AlignCenter().Column(col =>
                {
                    col.Item().Text("REJECTED QTY.").Bold().FontSize(8);
                    col.Item().Text(report.RejectedQty.ToString()).FontSize(9);
                });
                row.RelativeItem().Padding(3).AlignCenter().Column(col =>
                {
                    col.Item().Text("DEVIATION QTY.").Bold().FontSize(8);
                    col.Item().Text(report.DeviationQty.ToString()).FontSize(9);
                });
            });

            // Result and Signatures
            column.Item().PaddingTop(5).Border(1).Row(row =>
            {
                row.RelativeItem().BorderRight(1).Padding(3).Column(col =>
                {
                    col.Item().Text("Vendor Result").Bold().FontSize(8);
                    col.Item().Text(report.VendorResult ?? "").FontSize(8);
                    col.Item().PaddingTop(15).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Inspected By").FontSize(7);
                            c.Item().Text(report.InspectedBy ?? "").FontSize(8);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Approved By").FontSize(7);
                            c.Item().Text(report.ApprovedBy ?? "").FontSize(8);
                        });
                    });
                });
                row.RelativeItem().Padding(3).Column(col =>
                {
                    col.Item().Text("CIE Result").Bold().FontSize(8);
                    col.Item().Text(report.CieResult ?? "").FontSize(8);
                    col.Item().PaddingTop(15).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Inspected By").FontSize(7);
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Approved By").FontSize(7);
                        });
                    });
                });
            });
        });
    }
}
