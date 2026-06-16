using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Infrastructure.Notifications;

public sealed class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@wardkitten.com";
    public string FromName { get; set; } = "Wardkitten";
    public bool UseStartTls { get; set; } = true;
}

/// <summary>Canal Email vía SMTP (MailKit). Gratuito. Feature: F05.01.</summary>
public sealed class EmailChannel : INotificationChannel
{
    private readonly EmailOptions _options;

    public EmailChannel(IOptions<EmailOptions> options) => _options = options.Value;

    public ChannelType Channel => ChannelType.Email;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            return NotificationResult.Fail("SMTP no configurado.");

        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            mime.To.Add(MailboxAddress.Parse(message.Destination));
            mime.Subject = message.Title;

            var builder = new BodyBuilder { TextBody = BuildText(message), HtmlBody = BuildHtml(message) };
            mime.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port,
                _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
            if (!string.IsNullOrEmpty(_options.User))
                await client.AuthenticateAsync(_options.User, _options.Password, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            return NotificationResult.Ok();
        }
        catch (Exception ex)
        {
            return NotificationResult.Fail(ex.Message);
        }
    }

    private static string BuildText(NotificationMessage m)
    {
        var lines = new List<string> { m.Body };
        if (!string.IsNullOrEmpty(m.AckUrl)) lines.Add($"Marcar como hecho: {m.AckUrl}");
        return string.Join("\n\n", lines);
    }

    private static string BuildHtml(NotificationMessage m)
    {
        var actions = string.Concat(m.Actions.Select(a =>
            $"<a href=\"{a.Url}\" style=\"display:inline-block;padding:10px 16px;margin:4px;background:#6d28d9;color:#fff;border-radius:8px;text-decoration:none\">{a.Label}</a>"));
        return $"""
            <div style="font-family:system-ui,Arial,sans-serif;max-width:520px">
              <h2>🐾 {System.Net.WebUtility.HtmlEncode(m.Title)}</h2>
              <p>{System.Net.WebUtility.HtmlEncode(m.Body)}</p>
              <p>{actions}</p>
            </div>
            """;
    }
}
