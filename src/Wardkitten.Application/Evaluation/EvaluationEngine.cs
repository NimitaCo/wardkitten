using Microsoft.Extensions.Logging;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Evaluation;

/// <summary>
/// Motor de evaluación del watchdog. Es <b>recovery-safe</b> (al arrancar tras una caída recupera los
/// deadlines perdidos sin depender de timers en memoria) e <b>idempotente</b> en las alertas (un único
/// incidente abierto por watch). Lo orquesta el worker bajo leader election. Feature: F04.
/// </summary>
public sealed class EvaluationEngine
{
    private const int MissCatchUpGuard = 5000;

    private readonly IWatchRepository _watches;
    private readonly IIncidentRepository _incidents;
    private readonly NotificationDispatcher _dispatcher;
    private readonly IClock _clock;
    private readonly ILogger<EvaluationEngine> _logger;

    public EvaluationEngine(
        IWatchRepository watches,
        IIncidentRepository incidents,
        NotificationDispatcher dispatcher,
        IClock clock,
        ILogger<EvaluationEngine> logger)
    {
        _watches = watches;
        _incidents = incidents;
        _dispatcher = dispatcher;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Evalúa todos los watches vencidos. Devuelve cuántos se procesaron.</summary>
    public async Task<int> EvaluateDueAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var processed = 0;
        await foreach (var watch in _watches.StreamDueAsync(now, ct))
        {
            try
            {
                if (await EvaluateWatchAsync(watch, now, ct)) processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluando watch {WatchId}", watch.Id);
            }
        }
        return processed;
    }

    /// <summary>Evalúa un watch. Devuelve true si hubo cambios persistidos.</summary>
    public async Task<bool> EvaluateWatchAsync(Watch watch, DateTime now, CancellationToken ct = default)
    {
        if (!watch.IsActiveForEvaluation(now)) return false;

        var missed = false;
        var guard = 0;
        while (watch.IsPastDeadline(now) && guard++ < MissCatchUpGuard)
        {
            watch.RegisterMiss();
            var missedDue = watch.NextDueAtUtc!.Value;
            watch.ScheduleNextFrom(missedDue);
            missed = true;
            if (watch.NextDueAtUtc is null) break; // calendar agotado
        }

        if (missed)
        {
            if (watch.IsBreached)
            {
                var incident = await EnsureOpenIncidentAsync(watch, now, ct);
                watch.Status = WatchStatus.Down;
                watch.CurrentIncidentId = incident.Id;
                await _dispatcher.DispatchDueAsync(watch, incident, ct);
                await _incidents.ReplaceAsync(incident, ct);
            }
            else
            {
                watch.Status = WatchStatus.Grace;
            }
            await _watches.ReplaceAsync(watch, ct);
            return true;
        }

        // Vencido pero dentro de la gracia: refleja estado Grace (informativo) si aún estaba Up/New.
        if (watch.NextDueAtUtc <= now && watch.Status is WatchStatus.Up or WatchStatus.New)
        {
            watch.Status = WatchStatus.Grace;
            await _watches.ReplaceAsync(watch, ct);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reprocesa incidentes abiertos para avanzar el escalado (canales con retardo) y reintentar envíos
    /// pospuestos (quiet hours / saldo). Los reconocidos/resueltos quedan fuera por estado.
    /// </summary>
    public async Task<int> ProcessOpenIncidentsAsync(CancellationToken ct = default)
    {
        var processed = 0;
        await foreach (var incident in _incidents.StreamOpenAsync(ct))
        {
            try
            {
                var watch = await _watches.GetByIdAsync(incident.WatchId, ct);
                if (watch is null) continue;
                var before = incident.Deliveries.Count;
                await _dispatcher.DispatchDueAsync(watch, incident, ct);
                if (incident.Deliveries.Count != before)
                {
                    await _incidents.ReplaceAsync(incident, ct);
                    processed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalando incidente {IncidentId}", incident.Id);
            }
        }
        return processed;
    }

    private async Task<Incident> EnsureOpenIncidentAsync(Watch watch, DateTime now, CancellationToken ct)
    {
        var existing = await _incidents.GetOpenByWatchAsync(watch.Id, ct);
        if (existing is not null) return existing;

        var incident = new Incident
        {
            WatchId = watch.Id,
            UserId = watch.UserId,
            WatchName = watch.Name,
            Severity = watch.Severity,
            State = IncidentState.Open,
            OpenedAtUtc = now,
        };

        // El índice parcial único garantiza un solo incidente abierto por watch (manejado en el repo).
        return await _incidents.OpenOrGetExistingAsync(incident, ct);
    }
}
