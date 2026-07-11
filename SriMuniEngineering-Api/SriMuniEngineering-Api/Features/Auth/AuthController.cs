using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SriMuniEngineering_Api.Features.Auth.Dtos;

namespace SriMuniEngineering_Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        try
        {
            var response = await _authService.SignupAsync(request);
            return CreatedAtAction(nameof(Signup), new { id = response.UserId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPut("reset-credentials")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetCredentials([FromBody] ResetCredentialsRequest request)
    {
        try
        {
            await _authService.ResetCredentialsAsync(request);
            return Ok(new { message = "Credentials updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var exp = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (string.IsNullOrEmpty(jti))
            return BadRequest(new { message = "Invalid token." });

        var expiry = string.IsNullOrEmpty(exp)
            ? DateTime.Now.AddHours(8)
            : DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp)).UtcDateTime;

        _authService.Logout(jti, expiry);
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("UserId")?.Value 
                     ?? User.FindFirst("Id")?.Value;
                     
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid token claims." });

        try
        {
            var profile = await _authService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User profile not found." });
        }
    }
}
