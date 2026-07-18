using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Features.DeliveryChallans.Dtos;

namespace SriMuniEngineering_Api.Features.DeliveryChallans;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveryChallansController : ControllerBase
{
    private readonly DeliveryChallanService _service;

    public DeliveryChallansController(DeliveryChallanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryChallanResponse>>> GetAll()
    {
        var dcs = await _service.GetAllAsync();
        return Ok(dcs);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryChallanResponse>> GetById(Guid id)
    {
        var dc = await _service.GetByIdAsync(id);
        if (dc == null) return NotFound();
        return Ok(dc);
    }

    [HttpPost]
    public async Task<ActionResult<DeliveryChallanResponse>> Create(CreateDeliveryChallanRequest request)
    {
        var username = User.Identity?.Name ?? "System";
        var result = await _service.CreateAsync(request, username);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DeliveryChallanResponse>> Update(Guid id, CreateDeliveryChallanRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpGet("{id:guid}/generate-document")]
    public async Task<IActionResult> GenerateDocument(Guid id)
    {
        try
        {
            var pdfBytes = await _service.GeneratePdfAsync(id);
            var dc = await _service.GetByIdAsync(id);
            return File(pdfBytes, "application/pdf", $"DeliveryChallan_{dc?.DcNo ?? id.ToString()}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Delivery Challan not found");
        }
    }
}
