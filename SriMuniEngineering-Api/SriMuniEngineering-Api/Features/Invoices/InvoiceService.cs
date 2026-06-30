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

    public InvoiceService(AppDbContext context, SupabaseStorageService storageService, IConfiguration configuration)
    {
        _context = context;
        _storageService = storageService;
        _configuration = configuration;
    }

    public async Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request)
    {
        var ledger = await _context.JobWorkLedgers.FindAsync(request.DcLedgerId)
            ?? throw new KeyNotFoundException("DC Ledger entry not found.");

        if (ledger.Status != LedgerStatus.ReadyForInvoice)
            throw new InvalidOperationException("Ledger entry is not ready for invoicing.");

        var taxableValue = request.Quantity * request.Rate;
        var igstAmount = taxableValue * request.IgstRate / 100;
        var cgstAmount = taxableValue * request.CgstRate / 100;
        var sgstAmount = taxableValue * request.SgstRate / 100;
        var totalAmount = taxableValue + igstAmount + cgstAmount + sgstAmount;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNo = request.InvoiceNo,
            Date = request.Date,
            DcLedgerId = request.DcLedgerId,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Rate = request.Rate,
            TaxableValue = taxableValue,
            IgstRate = request.IgstRate,
            IgstAmount = igstAmount,
            CgstRate = request.CgstRate,
            CgstAmount = cgstAmount,
            SgstRate = request.SgstRate,
            SgstAmount = sgstAmount,
            TotalAmount = totalAmount,
            AmountInWords = ConvertToWords(totalAmount),
            DeliveryNoteNo = request.DeliveryNoteNo,
            ReferenceNo = request.ReferenceNo,
            BuyersOrderNo = request.BuyersOrderNo,
            DispatchDocNo = request.DispatchDocNo,
            Destination = request.Destination,
            TermsOfDelivery = request.TermsOfDelivery,
            AsnNo = request.AsnNo,
            EwbNo = request.EwbNo,
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);

        // Update ledger status
        ledger.Status = LedgerStatus.Invoiced;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(invoice.Id);
    }

    public async Task<InvoiceResponse> GetByIdAsync(Guid id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Product)
            .Include(i => i.DcLedger)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"Invoice with ID {id} not found.");

        return MapToResponse(invoice);
    }

    public async Task<PaginatedResponse<InvoiceResponse>> GetAllAsync(InvoiceFilterRequest filter)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Product)
            .AsQueryable();

        // ─── Filters ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
            query = query.Where(i => i.InvoiceNo.Contains(filter.InvoiceNo));

        if (filter.CustomerId.HasValue)
            query = query.Where(i => i.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue)
            query = query.Where(i => i.ProductId == filter.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            query = query.Where(i => i.Customer.Name.Contains(filter.CustomerName));

        if (!string.IsNullOrWhiteSpace(filter.PartNo))
            query = query.Where(i => i.Product.PartNo.Contains(filter.PartNo));

        if (filter.FromDate.HasValue)
            query = query.Where(i => i.Date >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(i => i.Date <= filter.ToDate.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(i => i.TotalAmount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(i => i.TotalAmount <= filter.MaxAmount.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(i =>
                i.InvoiceNo.Contains(filter.Search) ||
                i.Customer.Name.Contains(filter.Search) ||
                i.Product.PartNo.Contains(filter.Search) ||
                i.Product.PartName.Contains(filter.Search));

        // ─── Sorting ──────────────────────────────────────────
        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "invoiceno" => isAsc ? query.OrderBy(i => i.InvoiceNo) : query.OrderByDescending(i => i.InvoiceNo),
            "date" => isAsc ? query.OrderBy(i => i.Date) : query.OrderByDescending(i => i.Date),
            "customer" => isAsc ? query.OrderBy(i => i.Customer.Name) : query.OrderByDescending(i => i.Customer.Name),
            "amount" => isAsc ? query.OrderBy(i => i.TotalAmount) : query.OrderByDescending(i => i.TotalAmount),
            "partno" => isAsc ? query.OrderBy(i => i.Product.PartNo) : query.OrderByDescending(i => i.Product.PartNo),
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

    public async Task<string> GeneratePdfAsync(Guid id, string baseUrl)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Product)
            .Include(i => i.DcLedger)
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

    private InvoiceResponse MapToResponse(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNo = invoice.InvoiceNo,
        Date = invoice.Date,
        DcLedgerId = invoice.DcLedgerId,
        CustomerId = invoice.CustomerId,
        CustomerName = invoice.Customer.Name,
        ProductId = invoice.ProductId,
        PartNo = invoice.Product.PartNo,
        PartName = invoice.Product.PartName,
        HsnSac = invoice.Product.HsnSac,
        Quantity = invoice.Quantity,
        Rate = invoice.Rate,
        TaxableValue = invoice.TaxableValue,
        IgstRate = invoice.IgstRate,
        IgstAmount = invoice.IgstAmount,
        CgstRate = invoice.CgstRate,
        CgstAmount = invoice.CgstAmount,
        SgstRate = invoice.SgstRate,
        SgstAmount = invoice.SgstAmount,
        TotalAmount = invoice.TotalAmount,
        AmountInWords = invoice.AmountInWords,
        DeliveryNoteNo = invoice.DeliveryNoteNo,
        ReferenceNo = invoice.ReferenceNo,
        BuyersOrderNo = invoice.BuyersOrderNo,
        DispatchDocNo = invoice.DispatchDocNo,
        Destination = invoice.Destination,
        TermsOfDelivery = invoice.TermsOfDelivery,
        AsnNo = invoice.AsnNo,
        EwbNo = invoice.EwbNo,
        DownloadUrl = invoice.StoredFilePath != null ? $"/api/files/invoice/{Uri.EscapeDataString(invoice.InvoiceNo)}.pdf" : null,
        CreatedAt = invoice.CreatedAt
    };

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
