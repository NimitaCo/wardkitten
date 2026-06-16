using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Infrastructure.Notifications;

public sealed class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
}

/// <summary>
/// Canal Telegram vía Bot API (HTTP directo, sin SDK). Incluye botones inline ACK/Hecho/Snooze.
/// Gratuito. Feature: F05.01.
/// </summary>
public sealed class TelegramChannel : INotificationChannel
{
    private readonly TelegramOptions _options;
    private readonly IHttpClientFactory _httpFactory;

    public TelegramChannel(IOptions<TelegramOptions> options, IHttpClientFactory httpFactory)
    {
        _options = options.Value;
        _httpFactory = httpFactory;
    }

    public ChannelType Channel => ChannelType.Telegram;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
            return NotificationResult.Fail("Bot de Telegram no configurado.");

        try
        {
            var inlineKeyboard = message.Actions
                .Select(a => new[] { new { text = a.Label, url = a.Url } })
                .ToArray();

            var payload = new
            {
                chat_id = message.Destination,
                text = $"*{Escape(message.Title)}*\n\n{Escape(message.Body)}",
                parse_mode = "Markdown",
                reply_markup = inlineKeyboard.Length > 0 ? new { inline_keyboard = inlineKeyboard } : null,
            };

            var client = _httpFactory.CreateClient(nameof(TelegramChannel));
            var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
            using var response = await client.PostAsJsonAsync(url, payload, ct);
            if (!response.IsSuccessStatusCode)
                return NotificationResult.Fail($"Telegram HTTP {(int)response.StatusCode}");

            var result = await response.Content.ReadFromJsonAsync<TelegramResponse>(cancellationToken: ct);
            return NotificationResult.Ok(result?.Result?.MessageId.ToString());
        }
        catch (Exception ex)
        {
            return NotificationResult.Fail(ex.Message);
        }
    }

    private static string Escape(string s) => s.Replace("_", "\\_").Replace("*", "\\*");

    private sealed record TelegramResponse(bool Ok, TelegramMessage? Result);
    private sealed record TelegramMessage(long MessageId);
}
