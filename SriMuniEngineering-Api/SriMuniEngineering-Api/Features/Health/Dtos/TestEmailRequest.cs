using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Health.Dtos;

public class TestEmailRequest
{
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = "Test Email from Sri Muni Engineering";

    [Required]
    public string Body { get; set; } = "This is a test email to verify SMTP configuration.";
}
