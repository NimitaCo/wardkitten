using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.RealTime;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

/// <summary>
/// Registra check-ins (ping HTTP o confirmación manual) y actualiza el watch en consecuencia:
/// Success vuelve a poner el watch al día y resuelve cualquier incidente; Fail abre incidente y alerta;
/// Start solo se registra (para medir procesos largos). Feature: F03.
/// </summary>
public sealed class CheckInService
{
    private readonly IWatchRepository _watches;
    private readonly ICheckInRepository _checkIns;
    private readonly IncidentService _incidents;
    private readonly IWatchEventPublisher _events;
    private readonly IClock _clock;

    public CheckInService(
        IWatchRepository watches,
        ICheckInRepository checkIns,
        IncidentService incidents,
        IWatchEventPublisher events,
        IClock clock)
    {
        _watches = watches;
        _checkIns = checkIns;
        _incidents = incidents;
        _events = events;
        _clock = clock;
    }

    public async Task<Result> RecordByPingTokenAsync(string pingToken, CheckInKind kind, string? payload, string? ip, CancellationToken ct = default)
    {
        var watch = await _watches.GetByPingTokenAsync(pingToken, ct);
        if (watch is null) return Result.Fail("Token de ping inválido.");
        await RecordAsync(watch, kind, CheckInSource.Http, payload, ip, ct);
        return Result.Ok();
    }

    public async Task<Result> RecordManualAsync(string watchId, string userId, CheckInSource source, CancellationToken ct = default)
    {
        var watch = await _watches.GetByIdAsync(watchId, ct);
        if (watch is null || watch.UserId != userId) return Result.Fail("Watch no encontrado.");
        await RecordAsync(watch, CheckInKind.Success, source, null, null, ct);
        return Result.Ok();
    }

    private async Task RecordAsync(Watch watch, CheckInKind kind, CheckInSource source, string? payload, string? ip, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        await _checkIns.InsertAsync(new CheckIn
        {
            WatchId = watch.Id,
            UserId = watch.UserId,
            Kind = kind,
            Source = source,
            ReceivedAtUtc = now,
            Payload = payload,
            RemoteIp = ip,
        }, ct);

        switch (kind)
        {
            case CheckInKind.Success:
                watch.RegisterCheckIn(now);
                await _watches.ReplaceAsync(watch, ct);
                await _incidents.ResolveOpenForWatchAsync(watch.Id, "Check-in recibido", ct);
                await _events.WatchUpdatedAsync(watch, ct);
                break;

            case CheckInKind.Fail:
                // El proceso reportó fallo explícito: alerta inmediata.
                await _incidents.OpenAndAlertAsync(watch, ct);
                await _events.WatchUpdatedAsync(watch, ct);
                break;

            case CheckInKind.Start:
                // Solo se registra; la finalización (Success/Fail) llegará después.
                break;
        }
    }
}
