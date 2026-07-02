using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Features.Health.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Email;
using SriMuniEngineering_Api.Infrastructure.Storage;

namespace SriMuniEngineering_Api.Features.Health;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SupabaseStorageService _supabaseService;
    private readonly EmailService _emailService;

    public HealthController(AppDbContext context, SupabaseStorageService supabaseService, EmailService emailService)
    {
        _context = context;
        _supabaseService = supabaseService;
        _emailService = emailService;
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

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            await _emailService.SendAsync(request.To, request.Subject, request.Body);
            return Ok(new { message = $"Email successfully sent to {request.To}." });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = "Failed to send email.", detail = ex.Message });
        }
    }
}
