using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.RealTime;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

/// <summary>Datos de la solicitud HTTP que llega a la URL de ping.</summary>
public sealed record PingRequest(
    CheckInKind Kind,
    string? Payload = null,
    string? RemoteIp = null,
    string? Method = null,
    string? UserAgent = null);

/// <summary>Qué se hizo con un ping.</summary>
public enum PingResolution
{
    /// <summary>El token no corresponde a ninguna vigilancia ni a ninguna prueba en curso.</summary>
    NotFound = 0,

    /// <summary>Check-in real: reinicia contadores y resuelve incidentes.</summary>
    Recorded = 1,

    /// <summary>Solicitud de prueba: se registra en el banco de pruebas y no cuenta. Feature: F03.03.</summary>
    Test = 2,
}

/// <summary>
/// Registra check-ins (ping HTTP o confirmación manual) y actualiza el watch en consecuencia:
/// Success vuelve a poner el watch al día y resuelve cualquier incidente; Fail abre incidente y alerta;
/// Start solo se registra (para medir procesos largos). Feature: F03.
/// </summary>
public sealed class CheckInService
{
    private readonly IWatchRepository _watches;
    private readonly ICheckInRepository _checkIns;
    private readonly IPingProbeRepository _probes;
    private readonly IncidentService _incidents;
    private readonly IWatchEventPublisher _events;
    private readonly IClock _clock;

    public CheckInService(
        IWatchRepository watches,
        ICheckInRepository checkIns,
        IPingProbeRepository probes,
        IncidentService incidents,
        IWatchEventPublisher events,
        IClock clock)
    {
        _watches = watches;
        _checkIns = checkIns;
        _probes = probes;
        _incidents = incidents;
        _events = events;
        _clock = clock;
    }

    /// <summary>
    /// Resuelve un ping por token. Orden deliberado: primero la vigilancia real (camino de producción, una
    /// sola consulta) y solo si está en modo prueba —o si el token aún no pertenece a ninguna vigilancia—
    /// se busca el banco de pruebas. Ante la duda se cuenta el ping: perder un check-in real es peor.
    /// </summary>
    public async Task<PingResolution> RecordByPingTokenAsync(string pingToken, PingRequest request, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var watch = await _watches.GetByPingTokenAsync(pingToken, ct);

        if (watch is not null && !watch.IsTestMode(now))
        {
            await RecordAsync(watch, request.Kind, CheckInSource.Http, request.Payload, request.RemoteIp, ct);
            return PingResolution.Recorded;
        }

        var probe = await _probes.GetByTokenAsync(pingToken, ct);
        if (probe is not null && probe.IsActive(now))
        {
            var hit = new PingProbeHit
            {
                ReceivedAtUtc = now,
                Kind = request.Kind,
                Method = string.IsNullOrWhiteSpace(request.Method) ? "GET" : request.Method!,
                RemoteIp = request.RemoteIp,
                UserAgent = request.UserAgent,
                Payload = PingProbe.TrimPayload(request.Payload),
            };
            if (await _probes.RegisterHitAsync(probe.Id, hit, now, ct)) return PingResolution.Test;
        }

        // Ventana de prueba marcada pero sin banco vivo (caducó o se borró): vuelve a contar como real.
        if (watch is not null)
        {
            watch.EndTestMode(now);
            await RecordAsync(watch, request.Kind, CheckInSource.Http, request.Payload, request.RemoteIp, ct);
            return PingResolution.Recorded;
        }

        return PingResolution.NotFound;
    }

    public async Task<Result> RecordManualAsync(string watchId, string userId, CheckInSource source, CancellationToken ct = default)
    {
        var watch = await _watches.GetByIdAsync(watchId, ct);
        if (watch is null || watch.UserId != userId) return Result.Fail("Watch no encontrado.");
        await RecordAsync(watch, CheckInKind.Success, source, null, null, ct);
        return Result.Ok();
    }

    /// <summary>Registra éxito por watchId sin contexto de usuario (lo usa el magic link "Hecho", ya firmado).</summary>
    public async Task<Result> RecordSuccessByWatchIdAsync(string watchId, CheckInSource source, CancellationToken ct = default)
    {
        var watch = await _watches.GetByIdAsync(watchId, ct);
        if (watch is null) return Result.Fail("Watch no encontrado.");
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
