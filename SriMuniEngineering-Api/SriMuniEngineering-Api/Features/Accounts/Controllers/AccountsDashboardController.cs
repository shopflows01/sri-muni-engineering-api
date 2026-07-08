using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Common.Dtos;
using SriMuniEngineering_Api.Features.Accounts.Services;

namespace SriMuniEngineering_Api.Features.Accounts.Controllers;

[ApiController]
[Route("api/accounts/dashboard")]
public class AccountsDashboardController : ControllerBase
{
    private readonly IAccountsDashboardService _dashboardService;
    private readonly IInvoiceStatusService _invoiceStatusService;

    public AccountsDashboardController(IAccountsDashboardService dashboardService, IInvoiceStatusService invoiceStatusService)
    {
        _dashboardService = dashboardService;
        _invoiceStatusService = invoiceStatusService;
    }

    [HttpGet("invoices-summary")]
    public async Task<IActionResult> GetInvoiceSummary()
    {
        var result = await _dashboardService.GetInvoiceSummaryAsync();
        return Ok(result);
    }

    [HttpGet("customer-outstanding")]
    public async Task<IActionResult> GetCustomerOutstanding([FromQuery] PaginationRequest pagination)
    {
        var result = await _dashboardService.GetCustomerOutstandingAsync(pagination);
        return Ok(result);
    }

    [HttpGet("customer-outstanding/{customerId}")]
    public async Task<IActionResult> GetCustomerOutstandingDetail(Guid customerId)
    {
        var result = await _dashboardService.GetCustomerOutstandingDetailAsync(customerId);
        return Ok(result);
    }

    [HttpGet("invoices/status")]
    public async Task<IActionResult> GetInvoicesByStatus([FromQuery] Guid? customerId, [FromQuery] string? status, [FromQuery] PaginationRequest pagination)
    {
        var result = await _invoiceStatusService.GetInvoicesByStatusAsync(customerId, status, pagination);
        return Ok(result);
    }
}
