using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Common.Dtos;
using SriMuniEngineering_Api.Features.Accounts.Services;

namespace SriMuniEngineering_Api.Features.Accounts.Controllers;

[ApiController]
[Route("api/accounts/invoices")]
public class InvoiceStatusController : ControllerBase
{
    private readonly IInvoiceStatusService _invoiceStatusService;

    public InvoiceStatusController(IInvoiceStatusService invoiceStatusService)
    {
        _invoiceStatusService = invoiceStatusService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetInvoicesByStatus([FromQuery] Guid? customerId, [FromQuery] string? status, [FromQuery] PaginationRequest pagination)
    {
        var result = await _invoiceStatusService.GetInvoicesByStatusAsync(customerId, status, pagination);
        return Ok(result);
    }

    [HttpGet("{invoiceId}/status")]
    public async Task<IActionResult> GetStatus(Guid invoiceId)
    {
        try
        {
            var result = await _invoiceStatusService.GetStatusAsync(invoiceId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
