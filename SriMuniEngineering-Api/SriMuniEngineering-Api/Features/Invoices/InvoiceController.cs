using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SriMuniEngineering_Api.Features.Invoices;

[ApiController]
[Route("api/invoice")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly InvoiceService _invoiceService;

    public InvoiceController(InvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet("next-number")]
    public async Task<IActionResult> GetNextInvoiceNumber()
    {
        var result = await _invoiceService.GetNextInvoiceNumberAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dtos.CreateInvoiceRequest request)
    {
        try
        {
            var result = await _invoiceService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Dtos.UpdateInvoiceRequest request)
    {
        try
        {
            var result = await _invoiceService.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _invoiceService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Dtos.InvoiceFilterRequest filter)
    {
        var result = await _invoiceService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Generates the invoice PDF, uploads to storage, and returns the download URL.
    /// Single page, no labels.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var downloadUrl = await _invoiceService.GeneratePdfAsync(id, baseUrl);
            return Ok(new { downloadUrl });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Streams the invoice PDF directly as application/pdf with optional labels.
    /// Each true label adds a page. If none are true, generates single page without label.
    /// </summary>
    [HttpGet("{id:guid}/pdf/preview")]
    public async Task<IActionResult> PreviewPdf(
        Guid id,
        [FromQuery] bool originalForRecipient = false,
        [FromQuery] bool duplicateForTransporter = false,
        [FromQuery] bool triplicateForSupplier = false)
    {
        try
        {
            var (bytes, fileName) = await _invoiceService.GetPdfPreviewBytesAsync(
                id, originalForRecipient, duplicateForTransporter, triplicateForSupplier);
            return File(bytes, "application/pdf", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
