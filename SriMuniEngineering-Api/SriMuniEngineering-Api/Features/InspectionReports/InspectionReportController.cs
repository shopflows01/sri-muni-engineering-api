using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SriMuniEngineering_Api.Features.InspectionReports;

[ApiController]
[Route("api/inspection-report")]
[Authorize]
public class InspectionReportController : ControllerBase
{
    private readonly InspectionReportService _inspectionReportService;

    public InspectionReportController(InspectionReportService inspectionReportService)
    {
        _inspectionReportService = inspectionReportService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Dtos.CreateInspectionReportRequest request)
    {
        var result = await _inspectionReportService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _inspectionReportService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Dtos.InspectionReportFilterRequest filter)
    {
        var result = await _inspectionReportService.GetAllAsync(filter);
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
            var downloadUrl = await _inspectionReportService.GetPdfUrlAsync(id, baseUrl, regenerate);
            return Ok(new { downloadUrl });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
