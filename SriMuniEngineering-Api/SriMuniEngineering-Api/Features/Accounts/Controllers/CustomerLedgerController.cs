using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Common.Dtos;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Features.Accounts.Services;

namespace SriMuniEngineering_Api.Features.Accounts.Controllers;

[ApiController]
[Route("api/accounts/customers")]
public class CustomerLedgerController : ControllerBase
{
    private readonly ICustomerLedgerService _ledgerService;

    public CustomerLedgerController(ICustomerLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    [HttpGet("{customerId}/ledger")]
    public async Task<IActionResult> GetLedger(Guid customerId, [FromQuery] PaginationRequest pagination)
    {
        try
        {
            var result = await _ledgerService.GetLedgerAsync(customerId, pagination);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{customerId}/outstanding")]
    public async Task<IActionResult> GetOutstanding(Guid customerId)
    {
        var result = await _ledgerService.GetOutstandingAsync(customerId);
        return Ok(new { customerId, outstanding = result });
    }

    [HttpGet("{customerId}/advance")]
    public async Task<IActionResult> GetAdvanceBalance(Guid customerId)
    {
        var result = await _ledgerService.GetAdvanceBalanceAsync(customerId);
        return Ok(new { customerId, advanceBalance = result });
    }

    [HttpPost("ledger")]
    public async Task<IActionResult> CreateLedger([FromBody] CreateCustomerLedgerRequest request)
    {
        try
        {
            var result = await _ledgerService.CreateLedgerAsync(request.CustomerId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    [HttpGet("{customerId}/export-excel")]
    public async Task<IActionResult> ExportExcel(Guid customerId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var content = await _ledgerService.GenerateExcelLedgerAsync(customerId, fromDate, toDate);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = $"SalesRegister_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(content, contentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
