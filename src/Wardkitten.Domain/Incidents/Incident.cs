using Wardkitten.Domain.Common;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Domain.Incidents;

public enum IncidentState
{
    Open = 0,
    Acknowledged = 1,
    Resolved = 2,
}

public enum AlertDeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Skipped = 3,   // p.ej. quiet hours o saldo insuficiente
}

/// <summary>Un intento de entrega de alerta por un canal concreto en un escalón concreto.</summary>
public sealed class AlertDelivery
{
    public ChannelType Channel { get; set; }
    public string? Destination { get; set; }
    public int EscalationStep { get; set; }
    public AlertDeliveryStatus Status { get; set; } = AlertDeliveryStatus.Pending;
    public DateTime? SentAtUtc { get; set; }
    public decimal CreditsCharged { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Incidente abierto cuando un watch agota su tolerancia. Es la pieza que garantiza <b>idempotencia de
/// alertas</b>: existe como mucho un incidente abierto por watch, y cada (canal, escalón) se entrega una
/// sola vez. Feature: F04.01.
/// </summary>
public sealed class Incident : Entity
{
    public string WatchId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string WatchName { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.Medium;

    public IncidentState State { get; set; } = IncidentState.Open;

    public DateTime OpenedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolutionReason { get; set; }

    public int CurrentEscalationStep { get; set; }
    public DateTime? LastEscalatedAtUtc { get; set; }

    public List<AlertDelivery> Deliveries { get; set; } = new();

    public bool IsOpen => State == IncidentState.Open;

    /// <summary>
    /// ¿Ya se intentó la entrega por este canal en este escalón con resultado definitivo (enviado o
    /// fallido)? Los "Skipped" (quiet hours / saldo insuficiente) NO cuentan, para poder reintentar
    /// en un tick posterior cuando cambien las condiciones.
    /// </summary>
    public bool HasDispatched(ChannelType channel, int step)
        => Deliveries.Any(d => d.Channel == channel
                            && d.EscalationStep == step
                            && d.Status is AlertDeliveryStatus.Sent or AlertDeliveryStatus.Failed);

    public void Acknowledge(string by, DateTime nowUtc)
    {
        if (State == IncidentState.Resolved) return;
        State = IncidentState.Acknowledged;
        AcknowledgedBy = by;
        AcknowledgedAtUtc = nowUtc;
    }

    public void Resolve(string reason, DateTime nowUtc)
    {
        State = IncidentState.Resolved;
        ResolutionReason = reason;
        ResolvedAtUtc = nowUtc;
    }
}
