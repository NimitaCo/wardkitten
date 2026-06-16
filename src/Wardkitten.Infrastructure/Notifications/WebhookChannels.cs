using System.Net.Http.Json;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Infrastructure.Notifications;

/// <summary>
/// Integraciones salientes por HTTP (gratuitas). El destino es la URL configurada en el binding
/// (DestinationOverride). Webhook envía un JSON genérico; Slack/Discord usan su formato. Feature: F05.01.
/// </summary>
public sealed class WebhookChannel : INotificationChannel
{
    private readonly IHttpClientFactory _httpFactory;
    public WebhookChannel(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public ChannelType Channel => ChannelType.Webhook;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient(nameof(WebhookChannel));
            var payload = new
            {
                title = message.Title,
                body = message.Body,
                severity = message.Severity.ToString(),
                watchId = message.WatchId,
                incidentId = message.IncidentId,
                ackUrl = message.AckUrl,
            };
            using var response = await client.PostAsJsonAsync(message.Destination, payload, ct);
            return response.IsSuccessStatusCode
                ? NotificationResult.Ok()
                : NotificationResult.Fail($"Webhook HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex) { return NotificationResult.Fail(ex.Message); }
    }
}

public sealed class SlackChannel : INotificationChannel
{
    private readonly IHttpClientFactory _httpFactory;
    public SlackChannel(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public ChannelType Channel => ChannelType.Slack;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient(nameof(SlackChannel));
            var text = $"*{message.Title}*\n{message.Body}" + (string.IsNullOrEmpty(message.AckUrl) ? "" : $"\n<{message.AckUrl}|✅ Marcar como hecho>");
            using var response = await client.PostAsJsonAsync(message.Destination, new { text }, ct);
            return response.IsSuccessStatusCode
                ? NotificationResult.Ok()
                : NotificationResult.Fail($"Slack HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex) { return NotificationResult.Fail(ex.Message); }
    }
}

public sealed class DiscordChannel : INotificationChannel
{
    private readonly IHttpClientFactory _httpFactory;
    public DiscordChannel(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public ChannelType Channel => ChannelType.Discord;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient(nameof(DiscordChannel));
            var content = $"**{message.Title}**\n{message.Body}" + (string.IsNullOrEmpty(message.AckUrl) ? "" : $"\n{message.AckUrl}");
            using var response = await client.PostAsJsonAsync(message.Destination, new { content }, ct);
            return response.IsSuccessStatusCode
                ? NotificationResult.Ok()
                : NotificationResult.Fail($"Discord HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex) { return NotificationResult.Fail(ex.Message); }
    }
}

public sealed class MicrosoftTeamsChannel : INotificationChannel
{
    private readonly IHttpClientFactory _httpFactory;
    public MicrosoftTeamsChannel(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public ChannelType Channel => ChannelType.MicrosoftTeams;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient(nameof(MicrosoftTeamsChannel));
            var card = new Dictionary<string, object?>
            {
                ["@type"] = "MessageCard",
                ["@context"] = "https://schema.org/extensions",
                ["summary"] = message.Title,
                ["themeColor"] = "6d28d9",
                ["title"] = message.Title,
                ["text"] = message.Body,
            };
            if (!string.IsNullOrEmpty(message.AckUrl))
            {
                card["potentialAction"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "OpenUri",
                        ["name"] = "Marcar como hecho",
                        ["targets"] = new object[] { new Dictionary<string, string> { ["os"] = "default", ["uri"] = message.AckUrl! } },
                    },
                };
            }
            using var response = await client.PostAsJsonAsync(message.Destination, card, ct);
            return response.IsSuccessStatusCode
                ? NotificationResult.Ok()
                : NotificationResult.Fail($"Teams HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex) { return NotificationResult.Fail(ex.Message); }
    }
}
