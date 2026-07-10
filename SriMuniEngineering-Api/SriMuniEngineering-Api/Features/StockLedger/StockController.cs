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

    [HttpPost("dc")]
    public async Task<IActionResult> CreateDC([FromBody] CreateJobWorkDCRequest request)
    {
        var result = await _stockService.CreateDCAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDC(Guid id, [FromBody] UpdateJobWorkDCRequest request)
    {
        try
        {
            var result = await _stockService.UpdateDCAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("dc-item/{dcItemId:guid}/transaction")]
    public async Task<IActionResult> AddTransaction(Guid dcItemId, [FromBody] TransactionRequest request)
    {
        try
        {
            var result = await _stockService.AddTransactionAsync(dcItemId, request);
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _stockService.DeleteAsync(id);
            return NoContent();
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

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilterRequest filter)
    {
        var result = await _stockService.GetTransactionsAsync(filter);
        return Ok(result);
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] StockExportQuery query)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var downloadUrl = await _stockService.ExportToExcelAsync(query, baseUrl);
        return Ok(new { downloadUrl });
    }
}
