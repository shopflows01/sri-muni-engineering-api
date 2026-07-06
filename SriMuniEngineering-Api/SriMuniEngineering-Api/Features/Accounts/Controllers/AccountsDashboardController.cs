using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Features.Accounts.Services;

namespace SriMuniEngineering_Api.Features.Accounts.Controllers;

[ApiController]
[Route("api/accounts/dashboard")]
public class AccountsDashboardController : ControllerBase
{
    private readonly IAccountsDashboardService _dashboardService;

    public AccountsDashboardController(IAccountsDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("invoices-summary")]
    public async Task<IActionResult> GetInvoiceSummary()
    {
        var result = await _dashboardService.GetInvoiceSummaryAsync();
        return Ok(result);
    }

    [HttpGet("customer-outstanding")]
    public async Task<IActionResult> GetCustomerOutstanding()
    {
        var result = await _dashboardService.GetCustomerOutstandingAsync();
        return Ok(result);
    }

    [HttpGet("customer-outstanding/{customerId}")]
    public async Task<IActionResult> GetCustomerOutstandingDetail(Guid customerId)
    {
        var result = await _dashboardService.GetCustomerOutstandingDetailAsync(customerId);
        return Ok(result);
    }
}
