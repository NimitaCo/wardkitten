using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Notifications;

namespace Wardkitten.Infrastructure.Security;

public sealed class MagicLinkOptions
{
    public string Secret { get; set; } = string.Empty;
    public int TtlMinutes { get; set; } = 60 * 24; // 24h
}

/// <summary>
/// Magic links firmados con HMAC-SHA256 y expiración, para ACK/Hecho/Snooze desde la notificación.
/// Formato: <c>{base}/a/{base64url(payload)}.{base64url(sig)}</c>. Ver SECURITY.md §3.
/// </summary>
public sealed class HmacMagicLinkService : IAckLinkBuilder, IMagicLinkValidator
{
    private readonly MagicLinkOptions _options;
    private readonly NotificationOptions _notify;
    private readonly IClock _clock;

    public HmacMagicLinkService(IOptions<MagicLinkOptions> options, IOptions<NotificationOptions> notify, IClock clock)
    {
        _options = options.Value;
        _notify = notify.Value;
        _clock = clock;
    }

    public string BuildActionUrl(string incidentId, string watchId, string action)
    {
        var expiry = _clock.UtcNow.AddMinutes(_options.TtlMinutes).ToUnixTimeSeconds();
        var payload = $"{incidentId}|{watchId}|{action}|{expiry}";
        var token = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(Sign(payload))}";
        return $"{_notify.PublicBaseUrl.TrimEnd('/')}/a/{token}";
    }

    public MagicLinkData? Validate(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2) return null;

            var payloadBytes = Base64UrlDecode(parts[0]);
            var payload = Encoding.UTF8.GetString(payloadBytes);
            var expected = Sign(payload);
            var provided = Base64UrlDecode(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(expected, provided)) return null;

            var fields = payload.Split('|');
            if (fields.Length != 4) return null;
            if (!long.TryParse(fields[3], out var expiryUnix)) return null;
            if (DateTimeOffset.FromUnixTimeSeconds(expiryUnix) < _clock.UtcNow) return null;

            return new MagicLinkData(fields[0], fields[1], fields[2]);
        }
        catch
        {
            return null;
        }
    }

    private byte[] Sign(string payload)
        => HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.Secret), Encoding.UTF8.GetBytes(payload));

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}

file static class DateTimeExtensions
{
    public static long ToUnixTimeSeconds(this DateTime utc)
        => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();
}
