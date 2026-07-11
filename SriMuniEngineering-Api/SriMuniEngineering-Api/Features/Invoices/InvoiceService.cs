using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Common;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Domain.Enums;
using SriMuniEngineering_Api.Features.Invoices.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Storage;

namespace SriMuniEngineering_Api.Features.Invoices;

public class InvoiceService
{
    private readonly AppDbContext _context;
    private readonly SupabaseStorageService _storageService;
    private readonly IConfiguration _configuration;
    private readonly Accounts.Services.AccountPostingService _accountPostingService;

    public InvoiceService(
        AppDbContext context, 
        SupabaseStorageService storageService, 
        IConfiguration configuration,
        Accounts.Services.AccountPostingService accountPostingService)
    {
        _context = context;
        _storageService = storageService;
        _configuration = configuration;
        _accountPostingService = accountPostingService;
    }

    // ─── Next Invoice Number Preview ─────────────────────────────
    public async Task<NextInvoiceNumberResponse> GetNextInvoiceNumberAsync()
    {
        var financialYear = FinancialYearHelper.GetCurrentFinancialYear();
        var maxSequence = await _context.Invoices
            .Where(i => i.FinancialYear == financialYear)
            .MaxAsync(i => (int?)i.InvoiceSequence) ?? 0;

        var nextSequence = maxSequence + 1;

        return new NextInvoiceNumberResponse
        {
            InvoiceNo = $"{nextSequence:D3}/{financialYear}",
            InvoiceSequence = nextSequence,
            FinancialYear = financialYear
        };
    }

    // ─── Create Invoice ──────────────────────────────────────────
    public async Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request)
    {
        // Generate invoice number from the invoice date
        var financialYear = FinancialYearHelper.GetFinancialYear(request.InvoiceDate);
        var maxSequence = await _context.Invoices
            .Where(i => i.FinancialYear == financialYear)
            .MaxAsync(i => (int?)i.InvoiceSequence) ?? 0;
        var nextSequence = maxSequence + 1;

        // Build invoice items and compute totals
        var invoiceItems = new List<InvoiceItem>();
        decimal subTotal = 0;
        decimal totalGst = 0;

        foreach (var itemReq in request.Items)
        {
            var lineAmount = (itemReq.Quantity * itemReq.Rate) - itemReq.Discount;
            var lineGst = lineAmount * itemReq.GSTPercent / 100;

            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                ProductId = itemReq.ProductId,
                Description = itemReq.Description,
                HsnCode = itemReq.HsnCode,
                Quantity = itemReq.Quantity,
                Rate = itemReq.Rate,
                Discount = itemReq.Discount,
                GSTPercent = itemReq.GSTPercent,
                GSTAmount = lineGst,
                Amount = lineAmount + lineGst
            };

            invoiceItems.Add(item);
            subTotal += lineAmount;
            totalGst += lineGst;
        }

        var grandTotal = subTotal + totalGst;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNo = $"{nextSequence:D3}/{financialYear}",
            InvoiceSequence = nextSequence,
            FinancialYear = financialYear,
            Date = request.InvoiceDate,
            CustomerId = request.CustomerId,
            SubTotal = subTotal,
            GSTAmount = totalGst,
            GrandTotal = grandTotal,
            AmountInWords = ConvertToWords(grandTotal),
            Remarks = request.Remarks,
            DeliveryNoteNo = request.DeliveryNoteNo,
            DcDate = request.DcDate,
            ReferenceNo = request.ReferenceNo,
            BuyersOrderNo = request.BuyersOrderNo,
            DispatchDocNo = request.DispatchDocNo,
            Destination = request.Destination,
            TermsOfDelivery = request.TermsOfDelivery,
            AsnNo = request.AsnNo,
            EwbNo = request.EwbNo,
            Status = InvoiceStatus.Unpaid,
            Items = invoiceItems,
            CreatedAt = DateTime.Now
        };

        _context.Invoices.Add(invoice);

        // ─── Update Stock Ledger (Outward Qty) ──────────────────────
        if (request.DcLedgerId.HasValue)
        {
            var dc = await _context.JobWorkDCs
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == request.DcLedgerId.Value);

            if (dc != null)
            {
                foreach (var invoiceItem in invoiceItems)
                {
                    var dcItem = dc.Items.FirstOrDefault(i => i.ProductId == invoiceItem.ProductId);
                    if (dcItem != null)
                    {
                        _context.JobWorkTransactions.Add(new JobWorkTransaction
                        {
                            Id = Guid.NewGuid(),
                            DcItemId = dcItem.Id,
                            TransactionDate = invoice.Date,
                            TransactionType = TransactionType.Outward,
                            Quantity = (int)invoiceItem.Quantity,
                            ReferenceNo = invoice.InvoiceNo,
                            Remarks = "Auto-generated from Invoice"
                        });
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        // ─── Post to Accounting Ledger ──────────────────────────────
        await _accountPostingService.PostSalesInvoiceAsync(invoice.Id, invoice.CustomerId, invoice.GrandTotal, invoice.InvoiceNo);

        return await GetByIdAsync(invoice.Id);
    }

    // ─── Update Invoice ──────────────────────────────────────────
    public async Task<InvoiceResponse> UpdateAsync(Guid id, UpdateInvoiceRequest request)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");

        // Revert old outward quantities
        if (!string.IsNullOrEmpty(invoice.DeliveryNoteNo))
        {
            foreach (var oldItem in invoice.Items)
            {
                var transactionsToRemove = await _context.JobWorkTransactions
                    .Include(t => t.DcItem)
                        .ThenInclude(i => i.JobWorkDC)
                    .Where(t => t.DcItem.JobWorkDC.DcNo == invoice.DeliveryNoteNo 
                             && t.DcItem.ProductId == oldItem.ProductId
                             && t.ReferenceNo == invoice.InvoiceNo
                             && t.TransactionType == TransactionType.Outward)
                    .ToListAsync();
                    
                _context.JobWorkTransactions.RemoveRange(transactionsToRemove);
            }
        }

        // Remove existing items
        _context.InvoiceItems.RemoveRange(invoice.Items);

        // Build new items and compute totals
        var invoiceItems = new List<InvoiceItem>();
        decimal subTotal = 0;
        decimal totalGst = 0;

        foreach (var itemReq in request.Items)
        {
            var lineAmount = (itemReq.Quantity * itemReq.Rate) - itemReq.Discount;
            var lineGst = lineAmount * itemReq.GSTPercent / 100;

            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ProductId = itemReq.ProductId,
                Description = itemReq.Description,
                HsnCode = itemReq.HsnCode,
                Quantity = itemReq.Quantity,
                Rate = itemReq.Rate,
                Discount = itemReq.Discount,
                GSTPercent = itemReq.GSTPercent,
                GSTAmount = lineGst,
                Amount = lineAmount + lineGst
            };

            invoiceItems.Add(item);
            subTotal += lineAmount;
            totalGst += lineGst;
        }

        var grandTotal = subTotal + totalGst;

        invoice.Date = request.InvoiceDate;
        invoice.CustomerId = request.CustomerId;
        invoice.SubTotal = subTotal;
        invoice.GSTAmount = totalGst;
        invoice.GrandTotal = grandTotal;
        invoice.AmountInWords = ConvertToWords(grandTotal);
        invoice.Remarks = request.Remarks;
        invoice.DeliveryNoteNo = request.DeliveryNoteNo;
        invoice.DcDate = request.DcDate;
        invoice.ReferenceNo = request.ReferenceNo;
        invoice.BuyersOrderNo = request.BuyersOrderNo;
        invoice.DispatchDocNo = request.DispatchDocNo;
        invoice.Destination = request.Destination;
        invoice.TermsOfDelivery = request.TermsOfDelivery;
        invoice.AsnNo = request.AsnNo;
        invoice.EwbNo = request.EwbNo;

        // Clear cached PDF so it gets regenerated with updated data
        invoice.StoredFilePath = null;

        _context.InvoiceItems.AddRange(invoiceItems);

        // Apply new outward quantities
        if (request.DcLedgerId.HasValue)
        {
            var dc = await _context.JobWorkDCs
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == request.DcLedgerId.Value);

            if (dc != null)
            {
                foreach (var newItem in invoiceItems)
                {
                    var dcItem = dc.Items.FirstOrDefault(i => i.ProductId == newItem.ProductId);
                    if (dcItem != null)
                    {
                        _context.JobWorkTransactions.Add(new JobWorkTransaction
                        {
                            Id = Guid.NewGuid(),
                            DcItemId = dcItem.Id,
                            TransactionDate = invoice.Date,
                            TransactionType = TransactionType.Outward,
                            Quantity = (int)newItem.Quantity,
                            ReferenceNo = invoice.InvoiceNo,
                            Remarks = "Auto-generated from Invoice update"
                        });
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(request.DeliveryNoteNo))
        {
            foreach (var newItem in invoiceItems)
            {
                var dcItem = await _context.JobWorkDCItems
                    .Include(i => i.JobWorkDC)
                    .FirstOrDefaultAsync(i => i.JobWorkDC.DcNo == request.DeliveryNoteNo && i.ProductId == newItem.ProductId);
                    
                if (dcItem != null)
                {
                    _context.JobWorkTransactions.Add(new JobWorkTransaction
                    {
                        Id = Guid.NewGuid(),
                        DcItemId = dcItem.Id,
                        TransactionDate = invoice.Date,
                        TransactionType = TransactionType.Outward,
                        Quantity = (int)newItem.Quantity,
                        ReferenceNo = invoice.InvoiceNo,
                        Remarks = "Auto-generated from Invoice update"
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(invoice.Id);
    }

    // ─── Get By Id ───────────────────────────────────────────────
    public async Task<InvoiceResponse> GetByIdAsync(Guid id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");

        return MapToResponse(invoice);
    }

    // ─── Get All (Paginated) ─────────────────────────────────────
    public async Task<PaginatedResponse<InvoiceResponse>> GetAllAsync(InvoiceFilterRequest filter)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .AsQueryable();

        // ─── Filters ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
            query = query.Where(i => i.InvoiceNo.Contains(filter.InvoiceNo));

        if (filter.CustomerId.HasValue)
            query = query.Where(i => i.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue)
            query = query.Where(i => i.Items.Any(item => item.ProductId == filter.ProductId.Value));

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            query = query.Where(i => i.Customer.Name.Contains(filter.CustomerName));

        if (!string.IsNullOrWhiteSpace(filter.PartNo))
            query = query.Where(i => i.Items.Any(item => item.Product.PartNo.Contains(filter.PartNo)));

        if (filter.FromDate.HasValue)
            query = query.Where(i => i.Date >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(i => i.Date <= filter.ToDate.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(i => i.GrandTotal >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(i => i.GrandTotal <= filter.MaxAmount.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(i =>
                i.InvoiceNo.Contains(filter.Search) ||
                i.Customer.Name.Contains(filter.Search) ||
                i.Items.Any(item => item.Product.PartNo.Contains(filter.Search)) ||
                i.Items.Any(item => item.Product.PartName.Contains(filter.Search)));

        // ─── Sorting ──────────────────────────────────────────
        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "invoiceno" => isAsc ? query.OrderBy(i => i.InvoiceSequence) : query.OrderByDescending(i => i.InvoiceSequence),
            "date" => isAsc ? query.OrderBy(i => i.Date) : query.OrderByDescending(i => i.Date),
            "customer" => isAsc ? query.OrderBy(i => i.Customer.Name) : query.OrderByDescending(i => i.Customer.Name),
            "amount" => isAsc ? query.OrderBy(i => i.GrandTotal) : query.OrderByDescending(i => i.GrandTotal),
            _ => query.OrderByDescending(i => i.CreatedAt)
        };

        // ─── Pagination ───────────────────────────────────────
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResponse<InvoiceResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    // ─── Generate PDF (Upload to storage, return URL) ────────────
    public async Task<string> GeneratePdfAsync(Guid id, string baseUrl)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");

        var companyProfile = _configuration.GetSection("CompanyProfile");
        var pdfBytes = InvoicePdfGenerator.Generate(invoice, companyProfile);

        var fileName = $"{invoice.InvoiceNo.Replace("/", "-")}.pdf";
        var storedPath = await _storageService.UploadFileAsync("invoices", fileName, pdfBytes, "application/pdf");

        invoice.StoredFilePath = storedPath;
        await _context.SaveChangesAsync();

        return $"{baseUrl}/api/files/invoice/{Uri.EscapeDataString(invoice.InvoiceNo)}.pdf";
    }

    // ─── Preview PDF (Return bytes directly) ─────────────────────
    public async Task<(byte[] Bytes, string FileName)> GetPdfPreviewBytesAsync(
        Guid id, bool originalForRecipient, bool duplicateForTransporter, bool triplicateForSupplier)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");

        var companyProfile = _configuration.GetSection("CompanyProfile");
        var pdfBytes = InvoicePdfGenerator.Generate(
            invoice, companyProfile,
            originalForRecipient, duplicateForTransporter, triplicateForSupplier);

        var fileName = $"{invoice.InvoiceNo.Replace("/", "-")}.pdf";
        return (pdfBytes, fileName);
    }

    // ─── Mapping ─────────────────────────────────────────────────
    private InvoiceResponse MapToResponse(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNo = invoice.InvoiceNo,
        InvoiceSequence = invoice.InvoiceSequence,
        FinancialYear = invoice.FinancialYear,
        Date = invoice.Date,
        CustomerId = invoice.CustomerId,
        CustomerName = invoice.Customer.Name,
        SubTotal = invoice.SubTotal,
        GSTAmount = invoice.GSTAmount,
        GrandTotal = invoice.GrandTotal,
        AmountInWords = invoice.AmountInWords,
        Remarks = invoice.Remarks,
        DeliveryNoteNo = invoice.DeliveryNoteNo,
        DcDate = invoice.DcDate,
        ReferenceNo = invoice.ReferenceNo,
        BuyersOrderNo = invoice.BuyersOrderNo,
        DispatchDocNo = invoice.DispatchDocNo,
        Destination = invoice.Destination,
        TermsOfDelivery = invoice.TermsOfDelivery,
        AsnNo = invoice.AsnNo,
        EwbNo = invoice.EwbNo,
        DownloadUrl = invoice.StoredFilePath != null
            ? $"/api/files/invoice/{Uri.EscapeDataString(invoice.InvoiceNo)}.pdf"
            : null,
        Status = invoice.Status.ToString(),
        Items = invoice.Items.Select(item => new InvoiceItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            PartNo = item.Product.PartNo,
            PartName = item.Product.PartName,
            HsnSac = item.Product.HsnSac,
            Unit = item.Product.Unit,
            Description = item.Description,
            HsnCode = item.HsnCode,
            Quantity = item.Quantity,
            Rate = item.Rate,
            Discount = item.Discount,
            GSTPercent = item.GSTPercent,
            GSTAmount = item.GSTAmount,
            Amount = item.Amount
        }).ToList(),
        CreatedAt = invoice.CreatedAt
    };

    // ─── Amount to Words (Indian numbering) ──────────────────────
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
