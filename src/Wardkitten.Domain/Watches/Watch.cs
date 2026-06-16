using Wardkitten.Domain.Common;

namespace Wardkitten.Domain.Watches;

/// <summary>
/// Tarea/proceso vigilado (agregado raíz). Es el corazón del watchdog: define qué se espera, con qué
/// periodicidad y tolerancia, y por qué canales alertar si incumple. Feature: F02.01.
/// </summary>
public sealed class Watch : Entity
{
    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public WatchType Type { get; set; } = WatchType.Manual;

    public Schedule Schedule { get; set; } = new();
    public Tolerance Tolerance { get; set; } = new();

    /// <summary>Canales apilables y personalizables por tarea (ver <see cref="ChannelBinding"/>).</summary>
    public List<ChannelBinding> ChannelBindings { get; set; } = new();

    public Severity Severity { get; set; } = Severity.Medium;
    public List<string> Tags { get; set; } = new();
    public string? ProjectId { get; set; }

    public WatchStatus Status { get; set; } = WatchStatus.New;
    public bool Paused { get; set; }

    /// <summary>Momento (UTC) en que se espera el próximo check-in.</summary>
    public DateTime? NextDueAtUtc { get; set; }
    public DateTime? LastCheckInAtUtc { get; set; }

    /// <summary>Incumplimientos consecutivos acumulados (se compara contra la tolerancia a fallos).</summary>
    public int ConsecutiveMisses { get; set; }

    /// <summary>Token secreto e inadivinable de la URL de ping (ver SECURITY.md). Solo para tipo Ping.</summary>
    public string PingToken { get; set; } = string.Empty;

    public string? EscalationPolicyId { get; set; }

    /// <summary>Equipo al que escalar si el incidente sigue sin reconocer (on-call). Feature: F12.03.</summary>
    public string? EscalationTeamId { get; set; }
    /// <summary>Segundos desde la apertura del incidente antes de avisar a la persona de guardia del equipo.</summary>
    public int TeamEscalationDelaySeconds { get; set; }

    public List<MaintenanceWindow> MaintenanceWindows { get; set; } = new();

    /// <summary>Incidente abierto actualmente (garantiza idempotencia de alertas). Null si está OK.</summary>
    public string? CurrentIncidentId { get; set; }

    /// <summary>Racha actual de check-ins en plazo (gamificación). Feature: F10.01.</summary>
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }

    // ---- Lógica de dominio (pura, testeable) ----

    /// <summary>Deadline efectivo = próximo vencimiento + gracia.</summary>
    public DateTime? DeadlineUtc => NextDueAtUtc?.Add(Tolerance.Grace);

    public bool IsInMaintenance(DateTime nowUtc) => MaintenanceWindows.Any(w => w.Contains(nowUtc));

    /// <summary>¿Debe el motor evaluar este watch ahora mismo?</summary>
    public bool IsActiveForEvaluation(DateTime nowUtc)
        => !Paused
        && Status != WatchStatus.Paused
        && NextDueAtUtc.HasValue
        && !IsInMaintenance(nowUtc);

    /// <summary>¿Se ha superado el deadline (vencimiento + gracia)?</summary>
    public bool IsPastDeadline(DateTime nowUtc) => DeadlineUtc is { } d && nowUtc > d;

    /// <summary>¿Se ha agotado la tolerancia a fallos consecutivos?</summary>
    public bool IsBreached => Tolerance.IsBreached(ConsecutiveMisses);

    /// <summary>Reprograma el próximo vencimiento a partir del instante indicado.</summary>
    public void ScheduleNextFrom(DateTime fromUtc) => NextDueAtUtc = Schedule.ComputeNextDueUtc(fromUtc);

    /// <summary>Registra un check-in en plazo: limpia incumplimientos, vuelve a estado Up y reprograma.</summary>
    public void RegisterCheckIn(DateTime nowUtc)
    {
        LastCheckInAtUtc = nowUtc;
        ConsecutiveMisses = 0;
        CurrentStreak++;
        if (CurrentStreak > BestStreak) BestStreak = CurrentStreak;
        Status = WatchStatus.Up;
        CurrentIncidentId = null;
        ScheduleNextFrom(nowUtc);
    }

    /// <summary>Contabiliza un incumplimiento (un ciclo perdido). Rompe la racha en plazo.</summary>
    public void RegisterMiss()
    {
        ConsecutiveMisses++;
        CurrentStreak = 0;
    }

    public void Pause()
    {
        Paused = true;
        Status = WatchStatus.Paused;
    }

    public void Resume(DateTime nowUtc)
    {
        Paused = false;
        Status = WatchStatus.Up;
        ConsecutiveMisses = 0;
        ScheduleNextFrom(nowUtc);
    }
}
