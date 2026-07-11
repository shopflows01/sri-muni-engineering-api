using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Common.Dtos;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Features.Accounts.Services;

namespace SriMuniEngineering_Api.Features.Accounts.Controllers;

[ApiController]
[Route("api/accounts")]
public class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;

    public ReceiptsController(IReceiptService receiptService)
    {
        _receiptService = receiptService;
    }

    [HttpGet("receipts")]
    public async Task<IActionResult> GetReceipts([FromQuery] Guid? customerId, [FromQuery] PaginationRequest pagination)
    {
        var result = await _receiptService.GetReceiptsAsync(customerId, pagination);
        return Ok(result);
    }

    [HttpGet("receipts/{receiptVoucherId}")]
    public async Task<IActionResult> GetReceipt(Guid receiptVoucherId)
    {
        var result = await _receiptService.GetReceiptByIdAsync(receiptVoucherId);
        if (result == null) return NotFound(new { message = "Receipt not found." });
        return Ok(result);
    }

    [HttpPost("receipts")]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest request)
    {
        try
        {
            var voucher = await _receiptService.CreateReceiptAsync(request);
            return Ok(new { message = "Receipt created successfully", voucherId = voucher.Id, voucherNumber = voucher.VoucherNumber });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("receipts/{receiptVoucherId}/allocate")]
    public async Task<IActionResult> Allocate(Guid receiptVoucherId, [FromBody] AllocateRequest request)
    {
        try
        {
            await _receiptService.AllocateAsync(receiptVoucherId, request);
            return Ok(new { message = "Allocation successful" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An internal server error occurred.", details = ex.Message });
        }
    }

    [HttpDelete("allocations/{allocationId}")]
    public async Task<IActionResult> DeleteAllocation(Guid allocationId)
    {
        try
        {
            await _receiptService.DeleteAllocationAsync(allocationId);
            return Ok(new { message = "Allocation deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("allocations/{allocationId}")]
    public async Task<IActionResult> UpdateAllocation(Guid allocationId, [FromBody] UpdateAllocationRequest request)
    {
        try
        {
            await _receiptService.UpdateAllocationAsync(allocationId, request);
            return Ok(new { message = "Allocation updated successfully" });
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

    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllocations([FromQuery] string? search, [FromQuery] PaginationRequest pagination)
    {
        var result = await _receiptService.GetAllocationsAsync(search, pagination);
        return Ok(result);
    }
}
