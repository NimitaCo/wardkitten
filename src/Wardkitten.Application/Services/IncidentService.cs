using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.RealTime;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

/// <summary>Apertura, reconocimiento (ACK), posposición (snooze) y resolución de incidentes. Feature: F04.</summary>
public sealed class IncidentService
{
    private readonly IIncidentRepository _incidents;
    private readonly IWatchRepository _watches;
    private readonly NotificationDispatcher _dispatcher;
    private readonly IWatchEventPublisher _events;
    private readonly IClock _clock;

    public IncidentService(
        IIncidentRepository incidents,
        IWatchRepository watches,
        NotificationDispatcher dispatcher,
        IWatchEventPublisher events,
        IClock clock)
    {
        _incidents = incidents;
        _watches = watches;
        _dispatcher = dispatcher;
        _events = events;
        _clock = clock;
    }

    /// <summary>Abre (o reutiliza) un incidente y dispara las alertas iniciales. Usado por check-ins de fallo.</summary>
    public async Task<Incident> OpenAndAlertAsync(Watch watch, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var incident = await _incidents.OpenOrGetExistingAsync(new Incident
        {
            WatchId = watch.Id,
            UserId = watch.UserId,
            WatchName = watch.Name,
            Severity = watch.Severity,
            State = IncidentState.Open,
            OpenedAtUtc = now,
        }, ct);

        watch.Status = WatchStatus.Down;
        watch.CurrentIncidentId = incident.Id;
        await _dispatcher.DispatchDueAsync(watch, incident, ct);
        await _incidents.ReplaceAsync(incident, ct);
        await _watches.ReplaceAsync(watch, ct);
        await _events.IncidentOpenedAsync(incident, ct);
        return incident;
    }

    public async Task ResolveOpenForWatchAsync(string watchId, string reason, CancellationToken ct = default)
    {
        var incident = await _incidents.GetOpenByWatchAsync(watchId, ct);
        if (incident is null) return;
        incident.Resolve(reason, _clock.UtcNow);
        await _incidents.ReplaceAsync(incident, ct);
        await _events.IncidentResolvedAsync(incident, ct);
    }

    public async Task<Result> AcknowledgeAsync(string incidentId, string by, CancellationToken ct = default)
    {
        var incident = await _incidents.GetByIdAsync(incidentId, ct);
        if (incident is null) return Result.Fail("Incidente no encontrado.");
        incident.Acknowledge(by, _clock.UtcNow);
        await _incidents.ReplaceAsync(incident, ct);
        return Result.Ok();
    }

    /// <summary>Pospone: reconoce el incidente y empuja el próximo vencimiento del watch.</summary>
    public async Task<Result> SnoozeAsync(string incidentId, TimeSpan duration, CancellationToken ct = default)
    {
        var incident = await _incidents.GetByIdAsync(incidentId, ct);
        if (incident is null) return Result.Fail("Incidente no encontrado.");
        var now = _clock.UtcNow;
        incident.Acknowledge("snooze", now);
        await _incidents.ReplaceAsync(incident, ct);

        var watch = await _watches.GetByIdAsync(incident.WatchId, ct);
        if (watch is not null)
        {
            watch.NextDueAtUtc = now.Add(duration);
            watch.Status = WatchStatus.Grace;
            await _watches.ReplaceAsync(watch, ct);
            await _events.WatchUpdatedAsync(watch, ct);
        }
        return Result.Ok();
    }

    public Task<IReadOnlyList<Incident>> GetByUserAsync(string userId, int skip, int take, CancellationToken ct = default)
        => _incidents.GetByUserAsync(userId, skip, take, ct);
}
