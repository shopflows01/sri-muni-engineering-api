using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Enums;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using ClosedXML.Excel;
using System.IO;

using SriMuniEngineering_Api.Common.Dtos;

namespace SriMuniEngineering_Api.Features.Accounts.Services;

public interface ICustomerLedgerService
{
    Task<CustomerLedgerDto> GetLedgerAsync(Guid customerId, PaginationRequest pagination);
    Task<decimal> GetOutstandingAsync(Guid customerId);
    Task<decimal> GetAdvanceBalanceAsync(Guid customerId);
    Task<CustomerLedgerDto> CreateLedgerAsync(Guid customerId, CreateCustomerLedgerRequest request);
    Task<byte[]> GenerateExcelLedgerAsync(Guid customerId, DateTime? fromDate, DateTime? toDate);
}

public class CustomerLedgerService : ICustomerLedgerService
{
    private readonly AppDbContext _context;

    public CustomerLedgerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerLedgerDto> GetLedgerAsync(Guid customerId, PaginationRequest pagination)
    {
        var outstanding = await GetOutstandingAsync(customerId);
        var advance = await GetAdvanceBalanceAsync(customerId);

        var ledger = await _context.CustomerLedgers
            .Include(l => l.Customer)
            .Include(l => l.VoucherEntries)
                .ThenInclude(ve => ve.Voucher)
            .FirstOrDefaultAsync(l => l.CustomerId == customerId);

        if (ledger == null)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) throw new KeyNotFoundException("Customer not found.");

            return new CustomerLedgerDto
            {
                CustomerId = customerId,
                LedgerNo = "N/A",
                CustomerName = customer.Name,
                OpeningBalance = 0,
                OpeningBalanceType = BalanceType.Debit.ToString(),
                CurrentBalance = 0,
                OutstandingAmount = outstanding,
                AdvanceAmount = advance,
                Entries = new PagedResponse<LedgerEntryDto>()
            };
        }

        decimal runningBalance = ledger.OpeningBalanceType == BalanceType.Debit 
            ? ledger.OpeningBalance 
            : -ledger.OpeningBalance;

        var entries = ledger.VoucherEntries
            .OrderBy(e => e.Voucher.VoucherDate)
            .ThenBy(e => e.CreatedDate)
            .Select(e => 
            {
                runningBalance += e.DebitAmount;
                runningBalance -= e.CreditAmount;

                return new LedgerEntryDto
                {
                    Date = e.Voucher.VoucherDate,
                    VoucherNumber = e.Voucher.VoucherNumber,
                    VoucherType = e.Voucher.VoucherType.ToString(),
                    Narration = e.Remarks ?? e.Voucher.Narration,
                    Debit = e.DebitAmount,
                    Credit = e.CreditAmount,
                    RunningBalance = runningBalance
                };
            }).ToList();

        decimal finalBalance = runningBalance;

        if (pagination.SortDescending)
        {
            entries.Reverse();
        }

        int count = entries.Count;
        var pagedData = entries.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        var pagedEntries = new PagedResponse<LedgerEntryDto>(pagedData, count, pagination.PageNumber, pagination.PageSize);

        return new CustomerLedgerDto
        {
            CustomerId = customerId,
            LedgerNo = ledger.LedgerNo,
            CustomerName = ledger.Customer.Name,
            OpeningBalance = ledger.OpeningBalance,
            OpeningBalanceType = ledger.OpeningBalanceType.ToString(),
            CurrentBalance = finalBalance,
            OutstandingAmount = outstanding,
            AdvanceAmount = advance,
            Entries = pagedEntries
        };
    }

    public async Task<decimal> GetOutstandingAsync(Guid customerId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId)
            .Select(i => new
            {
                i.GrandTotal,
                Allocated = _context.VoucherAllocations.Where(a => a.InvoiceId == i.Id).Sum(a => a.AllocatedAmount)
            })
            .ToListAsync();

        return invoices.Sum(i => i.GrandTotal - i.Allocated);
    }

    public async Task<decimal> GetAdvanceBalanceAsync(Guid customerId)
    {
        var receiptCredits = await _context.VoucherEntries
            .Where(e => e.CustomerLedgerId != null && e.CustomerLedger!.CustomerId == customerId && e.Voucher.VoucherType == VoucherType.Receipt && e.CreditAmount > 0)
            .Select(e => new
            {
                e.CreditAmount,
                Allocated = e.Allocations.Sum(a => a.AllocatedAmount)
            })
            .ToListAsync();

        return receiptCredits.Sum(r => r.CreditAmount - r.Allocated);
    }

    public async Task<CustomerLedgerDto> CreateLedgerAsync(Guid customerId, CreateCustomerLedgerRequest request)
    {
        var existing = await _context.CustomerLedgers.FirstOrDefaultAsync(l => l.CustomerId == customerId);
        if (existing != null)
        {
            throw new InvalidOperationException("Ledger already exists for this customer.");
        }

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) throw new KeyNotFoundException("Customer not found.");

        var count = await _context.CustomerLedgers.CountAsync();
        var ledgerNo = $"LE-{count + 1:D3}";

        var ledger = new Domain.Entities.CustomerLedger
        {
            Id = Guid.NewGuid(),
            LedgerNo = ledgerNo,
            CustomerId = customerId,
            OpeningBalance = request.OpeningBalance,
            OpeningBalanceType = request.OpeningBalanceType,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.CustomerLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        return await GetLedgerAsync(customerId, new PaginationRequest { PageSize = int.MaxValue });
    }

    public async Task<byte[]> GenerateExcelLedgerAsync(Guid customerId, DateTime? fromDate, DateTime? toDate)
    {
        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new KeyNotFoundException($"Customer not found.");

        var query = _context.Invoices.Where(i => i.CustomerId == customerId);
        
        var today = DateTime.Today;
        var startDate = fromDate ?? new DateTime(today.Year, today.Month, 1);
        var endDate = toDate ?? new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        var invoices = await query
            .Where(i => i.Date.Date >= startDate.Date && i.Date.Date <= endDate.Date)
            .Include(i => i.Items)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.InvoiceNo)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sales Register");

        // Row 1-4 Merge A-H
        var titleRange = ws.Range("A1:H4");
        titleRange.Merge();
        
        var richText = ws.Cell("A1").GetRichText();
        richText.ClearText();
        richText.AddText("From: ").SetFontName("Calibri").SetFontSize(11).SetFontColor(XLColor.Black).SetBold(false);
        richText.AddText("SRI VALLI INDUSTRIES").SetFontName("Calibri").SetFontSize(14).SetFontColor(XLColor.Purple).SetBold(true);
        richText.AddText(Environment.NewLine + "To: ").SetFontName("Calibri").SetFontSize(11).SetFontColor(XLColor.Black).SetBold(false);
        richText.AddText(customer.Name.ToUpper()).SetFontName("Calibri").SetFontSize(14).SetFontColor(XLColor.Purple).SetBold(true);
        
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell("A1").Style.Alignment.WrapText = true;

        // Insert Image
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "frontend", "public", "assets", "excel-logo.png");
        if (System.IO.File.Exists(logoPath))
        {
            var picture = ws.AddPicture(logoPath);
            picture.MoveTo(ws.Cell("A1"));
            picture.Scale(0.8);
        }

        // Row 5 Merge A-H
        var productRange = ws.Range("A5:H5");
        productRange.Merge();
        productRange.Value = $"Sales Register: {startDate:dd-MMM-yyyy} to {endDate:dd-MMM-yyyy}";
        productRange.Style.Font.Bold = true;
        productRange.Style.Font.FontSize = 12;
        productRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Row 6 Headers: sl.no, invoice date, invoice no, taxble amount, gst %, gst value, total amount, remarks.
        ws.Cell("A6").Value = "Sl.No";
        ws.Cell("B6").Value = "Invoice Date";
        ws.Cell("C6").Value = "Invoice No";
        ws.Cell("D6").Value = "Taxable Amount";
        ws.Cell("E6").Value = "GST %";
        ws.Cell("F6").Value = "GST Value";
        ws.Cell("G6").Value = "Total Amount";
        ws.Cell("H6").Value = "Remarks";
        
        var headerRange = ws.Range("A6:H6");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int currentRow = 7;
        int slNo = 1;
        decimal totalTaxable = 0;
        decimal totalGst = 0;
        decimal totalAmount = 0;

        foreach (var inv in invoices)
        {
            // Determine GST % from items.
            var gstPercentStr = inv.Items.Any() 
                ? string.Join(", ", inv.Items.Select(i => i.GSTPercent).Distinct().Select(g => $"{g:0.##}%"))
                : "18%";
                
            ws.Cell(currentRow, 1).Value = slNo++;
            ws.Cell(currentRow, 2).Value = inv.Date.ToString("dd-MMM-yyyy");
            ws.Cell(currentRow, 3).Value = inv.InvoiceNo;
            ws.Cell(currentRow, 4).Value = inv.SubTotal;
            ws.Cell(currentRow, 5).Value = gstPercentStr;
            ws.Cell(currentRow, 6).Value = inv.GSTAmount;
            ws.Cell(currentRow, 7).Value = inv.GrandTotal;
            ws.Cell(currentRow, 8).Value = inv.Remarks;
            
            totalTaxable += inv.SubTotal;
            totalGst += inv.GSTAmount;
            totalAmount += inv.GrandTotal;
            
            currentRow++;
        }

        // Totals Row
        ws.Cell(currentRow, 3).Value = "Total:";
        ws.Cell(currentRow, 3).Style.Font.Bold = true;
        ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        
        ws.Cell(currentRow, 4).Value = totalTaxable;
        ws.Cell(currentRow, 4).Style.Font.Bold = true;
        
        ws.Cell(currentRow, 6).Value = totalGst;
        ws.Cell(currentRow, 6).Style.Font.Bold = true;
        
        ws.Cell(currentRow, 7).Value = totalAmount;
        ws.Cell(currentRow, 7).Style.Font.Bold = true;

        // Apply Borders
        var dataRange = ws.Range($"A1:H{currentRow}");
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        ws.Columns(1, 8).AdjustToContents();
        ws.Column(8).Width = Math.Max(ws.Column(8).Width, 15);

        // Print settings
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 0);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
