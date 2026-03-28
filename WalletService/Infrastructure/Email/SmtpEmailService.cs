using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using WalletService.Application.Interfaces;

namespace WalletService.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendTransferConfirmationAsync(
        string toEmail, string subject, string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var host        = _config["Smtp:Host"]!;
            var port        = int.Parse(_config["Smtp:Port"]!);
            var enableSsl   = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");
            var username    = _config["Smtp:Username"]!;
            var password    = _config["Smtp:Password"]!;
            var fromName    = _config["Smtp:FromName"] ?? "ProjectWallet";
            var fromAddress = _config["Smtp:FromAddress"]!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            var socketOptions = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(host, port, socketOptions, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {ToEmail} — Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Email failures must NOT fail the transfer — log and swallow
            _logger.LogError(ex, "Failed to send email to {ToEmail}: {Message}", toEmail, ex.Message);
        }
    }
}
