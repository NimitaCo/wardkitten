using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Infrastructure.Notifications;

public sealed class TwilioOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string SmsFrom { get; set; } = string.Empty;
    public string WhatsAppFrom { get; set; } = string.Empty;
}

/// <summary>
/// Base de los canales Twilio (SMS/WhatsApp) vía API HTTP. Son <b>metered</b>: el dispatcher cobra la
/// wallet antes de enviar (ver F06). Feature: F05.01.
/// </summary>
public abstract class TwilioChannelBase : INotificationChannel
{
    private readonly TwilioOptions _options;
    private readonly IHttpClientFactory _httpFactory;

    protected TwilioChannelBase(IOptions<TwilioOptions> options, IHttpClientFactory httpFactory)
    {
        _options = options.Value;
        _httpFactory = httpFactory;
    }

    public abstract ChannelType Channel { get; }
    protected abstract string From { get; }
    protected abstract string FormatTo(string destination);

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccountSid) || string.IsNullOrWhiteSpace(_options.AuthToken) || string.IsNullOrWhiteSpace(From))
            return NotificationResult.Fail($"Twilio ({Channel}) no configurado.");

        try
        {
            var body = message.Body;
            if (!string.IsNullOrEmpty(message.AckUrl)) body += $"\nHecho: {message.AckUrl}";

            var form = new Dictionary<string, string>
            {
                ["To"] = FormatTo(message.Destination),
                ["From"] = From,
                ["Body"] = $"{message.Title}\n{body}",
            };

            var client = _httpFactory.CreateClient(nameof(TwilioChannelBase));
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json")
            {
                Content = new FormUrlEncodedContent(form),
            };
            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            using var response = await client.SendAsync(request, ct);
            var payload = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return NotificationResult.Fail($"Twilio HTTP {(int)response.StatusCode}: {payload}");

            return NotificationResult.Ok(TryExtractSid(payload));
        }
        catch (Exception ex)
        {
            return NotificationResult.Fail(ex.Message);
        }
    }

    private static string? TryExtractSid(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("sid", out var sid) ? sid.GetString() : null;
        }
        catch { return null; }
    }
}

public sealed class TwilioSmsChannel : TwilioChannelBase
{
    private readonly TwilioOptions _options;
    public TwilioSmsChannel(IOptions<TwilioOptions> options, IHttpClientFactory httpFactory) : base(options, httpFactory)
        => _options = options.Value;

    public override ChannelType Channel => ChannelType.Sms;
    protected override string From => _options.SmsFrom;
    protected override string FormatTo(string destination) => destination;
}

public sealed class TwilioWhatsAppChannel : TwilioChannelBase
{
    private readonly TwilioOptions _options;
    public TwilioWhatsAppChannel(IOptions<TwilioOptions> options, IHttpClientFactory httpFactory) : base(options, httpFactory)
        => _options = options.Value;

    public override ChannelType Channel => ChannelType.WhatsApp;
    protected override string From => string.IsNullOrWhiteSpace(_options.WhatsAppFrom) ? string.Empty : $"whatsapp:{_options.WhatsAppFrom}";
    protected override string FormatTo(string destination) => $"whatsapp:{destination}";
}
