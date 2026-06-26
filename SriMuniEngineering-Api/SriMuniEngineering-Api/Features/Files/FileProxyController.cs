using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Storage;

namespace SriMuniEngineering_Api.Features.Files;

[ApiController]
[Route("api/files")]
[AllowAnonymous]
public class FileProxyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SupabaseStorageService _storageService;

    public FileProxyController(AppDbContext context, SupabaseStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    [HttpGet("invoice/{invoiceNo}.pdf")]
    public async Task<IActionResult> DownloadInvoice(string invoiceNo)
    {
        var decodedNo = Uri.UnescapeDataString(invoiceNo);
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceNo == decodedNo);

        if (invoice is null)
            return NotFound(new { message = $"Invoice '{decodedNo}' not found." });

        if (string.IsNullOrEmpty(invoice.StoredFilePath))
            return NotFound(new { message = "Invoice PDF has not been generated yet." });

        try
        {
            var bytes = await _storageService.DownloadFileAsync(invoice.StoredFilePath);
            var fileName = $"{decodedNo.Replace("/", "-")}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = "Failed to retrieve file from storage.", detail = ex.Message });
        }
    }

    [HttpGet("quotation/{quotationNo}.pdf")]
    public async Task<IActionResult> DownloadQuotation(string quotationNo)
    {
        var decodedNo = Uri.UnescapeDataString(quotationNo);
        var quotation = await _context.Quotations
            .FirstOrDefaultAsync(q => q.QuotationNo == decodedNo);

        if (quotation is null)
            return NotFound(new { message = $"Quotation '{decodedNo}' not found." });

        if (string.IsNullOrEmpty(quotation.StoredFilePath))
            return NotFound(new { message = "Quotation PDF has not been generated yet." });

        try
        {
            var bytes = await _storageService.DownloadFileAsync(quotation.StoredFilePath);
            var fileName = $"{decodedNo.Replace("/", "-")}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = "Failed to retrieve file from storage.", detail = ex.Message });
        }
    }

    [HttpGet("inspection-report/{id:guid}.pdf")]
    public async Task<IActionResult> DownloadInspectionReport(Guid id)
    {
        var report = await _context.InspectionReports.FindAsync(id);

        if (report is null)
            return NotFound(new { message = $"Inspection report with ID {id} not found." });

        if (string.IsNullOrEmpty(report.StoredFilePath))
            return NotFound(new { message = "Inspection report PDF has not been generated yet." });

        try
        {
            var bytes = await _storageService.DownloadFileAsync(report.StoredFilePath);
            var fileName = $"IR-{id.ToString()[..8]}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = "Failed to retrieve file from storage.", detail = ex.Message });
        }
    }
}
