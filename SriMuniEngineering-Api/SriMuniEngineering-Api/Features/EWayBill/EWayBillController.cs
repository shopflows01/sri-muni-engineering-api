using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Features.EWayBill.Dtos;

namespace SriMuniEngineering_Api.Features.EWayBill;

[ApiController]
[Route("api/ewaybill")]
[Authorize]
public class EWayBillController : ControllerBase
{
    private readonly EWayBillService _ewayBillService;

    public EWayBillController(EWayBillService ewayBillService)
    {
        _ewayBillService = ewayBillService;
    }

    [HttpPost("payload-builder")]
    public async Task<IActionResult> BuildPayload([FromBody] EWayBillPayloadRequest request)
    {
        try
        {
            var payload = await _ewayBillService.BuildPayloadAsync(request.InvoiceIds);
            return Ok(payload);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
