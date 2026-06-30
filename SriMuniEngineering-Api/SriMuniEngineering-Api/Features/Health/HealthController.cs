using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Storage;

namespace SriMuniEngineering_Api.Features.Health;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SupabaseStorageService _supabaseService;

    public HealthController(AppDbContext context, SupabaseStorageService supabaseService)
    {
        _context = context;
        _supabaseService = supabaseService;
    }

    [HttpGet]
    public async Task<IActionResult> CheckHealth()
    {
        var sqlConnected = await _context.Database.CanConnectAsync();
        var supabaseConnected = await _supabaseService.PingAsync();

        var status = (sqlConnected && supabaseConnected) ? "Healthy" : "Degraded";

        var response = new
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            Dependencies = new
            {
                Database = sqlConnected ? "Connected" : "Disconnected",
                Supabase = supabaseConnected ? "Connected" : "Disconnected"
            }
        };

        if (status == "Healthy")
            return Ok(response);

        return StatusCode(503, response);
    }
}
