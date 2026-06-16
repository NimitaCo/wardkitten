using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Notifications;

/// <summary>Botón de acción rápida (ACK/Hecho/Snooze) para canales que lo soportan (Telegram, Push).</summary>
public sealed record NotificationAction(string Label, string Url, string Kind);

/// <summary>Mensaje a entregar por un canal concreto, con destino ya resuelto.</summary>
public sealed class NotificationMessage
{
    public required ChannelType Channel { get; init; }
    public required string Destination { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string? AckUrl { get; init; }
    public string? WatchId { get; init; }
    public string? IncidentId { get; init; }
    public Severity Severity { get; init; } = Severity.Medium;
    public IReadOnlyList<NotificationAction> Actions { get; init; } = Array.Empty<NotificationAction>();
}

public sealed class NotificationResult
{
    public bool Success { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? Error { get; init; }

    public static NotificationResult Ok(string? providerMessageId = null)
        => new() { Success = true, ProviderMessageId = providerMessageId };

    public static NotificationResult Fail(string error)
        => new() { Success = false, Error = error };
}

/// <summary>
/// Estrategia de canal de notificación. Los canales <see cref="IsMetered"/> (SMS/WhatsApp) consumen
/// créditos de la wallet; el dispatcher se encarga del cobro antes de enviar. Feature: F05.
/// </summary>
public interface INotificationChannel
{
    ChannelType Channel { get; }
    bool IsMetered => Channel.IsMetered();
    Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default);
}
