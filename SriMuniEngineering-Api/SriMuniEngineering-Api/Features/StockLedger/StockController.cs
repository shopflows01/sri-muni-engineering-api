using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Features.StockLedger.Dtos;

namespace SriMuniEngineering_Api.Features.StockLedger;

[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly StockService _stockService;

    public StockController(StockService stockService)
    {
        _stockService = stockService;
    }

    [HttpPost("inward")]
    public async Task<IActionResult> CreateInward([FromBody] InwardRequest request)
    {
        var result = await _stockService.CreateInwardAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("outward/{id:guid}")]
    public async Task<IActionResult> UpdateOutward(Guid id, [FromBody] OutwardRequest request)
    {
        try
        {
            var result = await _stockService.UpdateOutwardAsync(id, request);
            return Ok(result);
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _stockService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] StockFilterRequest filter)
    {
        var result = await _stockService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] StockExportQuery query)
    {
        var downloadUrl = await _stockService.ExportToExcelAsync(query);
        return Ok(new { downloadUrl });
    }
}
