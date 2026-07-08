using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Auth.Dtos;

public class ResetCredentialsRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? NewUsername { get; set; }
    public string? NewPassword { get; set; }
}
