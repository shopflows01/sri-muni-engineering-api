using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SriMuniEngineering_Api.Infrastructure.Email;

public class EmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _fromEmail;
    private readonly string _appPassword;

    public EmailService(IConfiguration configuration)
    {
        var smtp = configuration.GetSection("Smtp");
        _host = smtp["Host"]!;
        _port = int.Parse(smtp["Port"]!);
        _fromEmail = smtp["Email"]!;
        _appPassword = smtp["AppPassword"]!;
    }

    public async Task SendAsync(string toEmail, string subject, string body, bool isHtml = false)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart(isHtml ? "html" : "plain") { Text = body };

        using var client = new SmtpClient();

        // Connect with STARTTLS on port 587
        await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_fromEmail, _appPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
