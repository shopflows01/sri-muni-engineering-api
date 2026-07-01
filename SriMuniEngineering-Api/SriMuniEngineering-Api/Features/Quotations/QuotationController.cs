using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SriMuniEngineering_Api.Features.Quotations;

[ApiController]
[Route("api/quotation")]
[Authorize]
public class QuotationController : ControllerBase
{
    private readonly QuotationService _quotationService;

    public QuotationController(QuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dtos.CreateQuotationRequest request)
    {
        var result = await _quotationService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _quotationService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Dtos.QuotationFilterRequest filter)
    {
        var result = await _quotationService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Returns the download URL for an existing PDF. If not present (or if regenerate=true), generates and uploads it.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdfUrl(Guid id, [FromQuery] bool regenerate = false)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var downloadUrl = await _quotationService.GetPdfUrlAsync(id, baseUrl, regenerate);
            return Ok(new { downloadUrl });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Streams the PDF directly as application/pdf. Generates and uploads if not already stored.
    /// </summary>
    [HttpGet("{id:guid}/pdf/preview")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        try
        {
            var (bytes, fileName) = await _quotationService.GetPdfBytesAsync(id);
            return File(bytes, "application/pdf", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
