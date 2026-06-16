namespace Wardkitten.Application.Notifications;

/// <summary>
/// Construye magic links firmados para ACK/Hecho/Snooze desde la notificación. La implementación firma
/// el token (HMAC, un solo uso, expiración) — ver SECURITY.md. Feature: F05.03.
/// </summary>
public interface IAckLinkBuilder
{
    string BuildActionUrl(string incidentId, string watchId, string action);
}

public sealed class NotificationOptions
{
    /// <summary>URL pública base de la app para construir magic links y deep links (sin barra final).</summary>
    public string PublicBaseUrl { get; set; } = "https://app.wardkitten.com";
}
