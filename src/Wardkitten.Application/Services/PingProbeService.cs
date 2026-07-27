// Feature: F03.03 — banco de pruebas de la URL de ping (dry-run en alta/edición)
using Microsoft.Extensions.Options;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.RealTime;
using Wardkitten.Application.Security;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

/// <summary>Una solicitud llegada a la URL, ya sea de prueba (<c>Counted = false</c>) o real.</summary>
public sealed record PingActivity(
    DateTime ReceivedAtUtc,
    CheckInKind Kind,
    string Source,
    bool Counted,
    string? Method = null,
    string? RemoteIp = null,
    string? UserAgent = null,
    string? Payload = null);

/// <summary>Estado del banco de pruebas que consume la pantalla de alta/edición.</summary>
public sealed record PingTestState(
    string ProbeId,
    string Token,
    string Url,
    PingProbeMode Mode,
    DateTime ExpiresAtUtc,
    DateTime? TestModeUntilUtc,
    DateTime? LastHitAtUtc,
    int HitCount,
    IReadOnlyList<PingActivity> Activity);

/// <summary>
/// Banco de pruebas de la URL de ping (F03.03): permite comprobar durante el alta o la edición que el
/// sistema remoto llega, <b>sin que esas solicitudes cuenten</b>.
/// <list type="bullet">
/// <item><b>Draft</b> — la vigilancia aún no tiene URL. El token se genera aquí y se adopta al guardar,
/// así que la URL con la que se ensaya es exactamente la que resetea contadores en producción.</item>
/// <item><b>DryRun</b> — la vigilancia ya existe: se abre una ventana corta durante la cual su URL real
/// no cuenta y, para que el ensayo no dispare alertas, tampoco se evalúan sus vencimientos.</item>
/// </list>
/// Los borradores que nadie termina se borran solos (índice TTL sobre <c>expiresAtUtc</c>).
/// </summary>
public sealed class PingProbeService
{
    /// <summary>Tope de bancos vivos por usuario; al superarlo se descartan los más antiguos.</summary>
    private const int MaxActiveProbesPerUser = 5;

    private const int MaxActivityItems = 50;

    private readonly IPingProbeRepository _probes;
    private readonly IWatchRepository _watches;
    private readonly ICheckInRepository _checkIns;
    private readonly IWatchEventPublisher _events;
    private readonly NotificationOptions _options;
    private readonly IClock _clock;

    public PingProbeService(
        IPingProbeRepository probes,
        IWatchRepository watches,
        ICheckInRepository checkIns,
        IWatchEventPublisher events,
        IOptions<NotificationOptions> options,
        IClock clock)
    {
        _probes = probes;
        _watches = watches;
        _checkIns = checkIns;
        _events = events;
        _options = options.Value;
        _clock = clock;
    }

    /// <summary>URL pública de ping de un token (la misma que usará el sistema remoto en producción).</summary>
    public string BuildPingUrl(string token) => $"{_options.PublicBaseUrl.TrimEnd('/')}/p/{token}";

    /// <summary>
    /// Abre un banco de pruebas. Sin <paramref name="watchId"/> (o si la vigilancia todavía no tiene URL)
    /// se reserva un token nuevo en modo borrador; si la vigilancia ya tiene URL, se ensaya sobre ella
    /// abriendo una ventana de dry-run acotada.
    /// </summary>
    public async Task<Result<PingTestState>> StartAsync(
        string userId, string? watchId, TimeSpan? dryRunWindow, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        Watch? watch = null;

        if (!string.IsNullOrEmpty(watchId))
        {
            watch = await _watches.GetByIdAsync(watchId, ct);
            if (watch is null || watch.UserId != userId) return Result<PingTestState>.Fail("Vigilancia no encontrada.");
        }

        var dryRun = watch is not null && !string.IsNullOrEmpty(watch.PingToken);
        var window = PingProbe.ClampDryRunWindow(dryRunWindow ?? PingProbe.DefaultDryRunWindow);

        var probe = watch is not null ? await _probes.GetByWatchAsync(watch.Id, ct) : null;
        if (probe is not null && (probe.UserId != userId || (dryRun && probe.Token != watch!.PingToken)))
        {
            // Banco heredado de otra configuración: no sirve para esta URL.
            await _probes.DeleteAsync(probe.Id, ct);
            probe = null;
        }

        if (probe is null)
        {
            probe = new PingProbe
            {
                UserId = userId,
                WatchId = watch?.Id,
                Mode = dryRun ? PingProbeMode.DryRun : PingProbeMode.Draft,
                Token = dryRun ? watch!.PingToken : SecureTokenGenerator.New(),
                ExpiresAtUtc = dryRun ? now + window + PingProbe.DryRunHistoryGrace : now + PingProbe.DraftLifetime,
            };
            await _probes.InsertAsync(probe, ct);
        }
        else
        {
            probe.Mode = dryRun ? PingProbeMode.DryRun : PingProbeMode.Draft;
            probe.ExpiresAtUtc = dryRun ? now + window + PingProbe.DryRunHistoryGrace : now + PingProbe.DraftLifetime;
            await _probes.ReplaceAsync(probe, ct);
        }

        if (dryRun)
        {
            watch!.StartTestMode(now + window);
            await _watches.ReplaceAsync(watch, ct);
            await _events.WatchUpdatedAsync(watch, ct);
        }

        await PruneAsync(userId, probe.Id, now, ct);
        return Result<PingTestState>.Ok(await BuildStateAsync(probe, watch, now, ct));
    }

    /// <summary>
    /// Estado actual del banco (última solicitud e histórico). Renueva la caducidad de los borradores:
    /// mientras la pantalla siga abierta el banco vive; en cuanto se abandona, el TTL lo borra.
    /// </summary>
    public async Task<Result<PingTestState>> GetAsync(string probeId, string userId, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var probe = await _probes.GetByIdAsync(probeId, ct);
        if (probe is null || probe.UserId != userId || !probe.IsActive(now))
            return Result<PingTestState>.Fail("La prueba ha caducado.");

        if (probe.Mode == PingProbeMode.Draft)
        {
            probe.ExpiresAtUtc = now + PingProbe.DraftLifetime;
            await _probes.ExtendAsync(probe.Id, probe.ExpiresAtUtc, ct);
        }

        var watch = probe.WatchId is null ? null : await _watches.GetByIdAsync(probe.WatchId, ct);
        return Result<PingTestState>.Ok(await BuildStateAsync(probe, watch, now, ct));
    }

    /// <summary>Termina la prueba: cierra el dry-run (la URL vuelve a contar) y borra el banco.</summary>
    public async Task<Result> StopAsync(string probeId, string userId, CancellationToken ct = default)
    {
        var probe = await _probes.GetByIdAsync(probeId, ct);
        if (probe is null) return Result.Ok();                       // ya caducó: nada que cerrar
        if (probe.UserId != userId) return Result.Fail("Prueba no encontrada.");

        await EndTestModeAsync(probe.WatchId, ct);
        await _probes.DeleteAsync(probe.Id, ct);
        return Result.Ok();
    }

    /// <summary>
    /// Toma el borrador para que la vigilancia adopte su token al guardar. Devuelve null si no es válido
    /// (caducado, de otro usuario o ya usado): en ese caso se generará un token nuevo.
    /// </summary>
    public async Task<PingProbe?> ClaimDraftAsync(string probeId, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(probeId)) return null;
        var probe = await _probes.GetByIdAsync(probeId, ct);
        if (probe is null || probe.UserId != userId || probe.Mode != PingProbeMode.Draft) return null;
        return probe.IsActive(_clock.UtcNow) ? probe : null;
    }

    /// <summary>Liga el banco a la vigilancia ya guardada y conserva su historial un rato antes del TTL.</summary>
    public async Task BindAsync(PingProbe probe, string watchId, CancellationToken ct = default)
    {
        probe.WatchId = watchId;
        probe.ExpiresAtUtc = _clock.UtcNow + PingProbe.DryRunHistoryGrace;
        await _probes.ReplaceAsync(probe, ct);
    }

    /// <summary>Descarta el banco (p. ej. la vigilancia se guardó como manual y ya no hay URL que probar).</summary>
    public async Task DiscardAsync(string probeId, string userId, CancellationToken ct = default)
    {
        var probe = await _probes.GetByIdAsync(probeId, ct);
        if (probe is not null && probe.UserId == userId) await _probes.DeleteAsync(probe.Id, ct);
    }

    /// <summary>Borra los bancos de pruebas de una vigilancia (al eliminarla).</summary>
    public Task DeleteForWatchAsync(string watchId, CancellationToken ct = default)
        => _probes.DeleteByWatchAsync(watchId, ct);

    /// <summary>Cierra el dry-run de una vigilancia para que su URL vuelva a contar (al guardar o parar).</summary>
    public async Task EndTestModeAsync(string? watchId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(watchId)) return;
        var watch = await _watches.GetByIdAsync(watchId, ct);
        if (watch is null || watch.TestModeUntilUtc is null) return;

        watch.EndTestMode(_clock.UtcNow);
        await _watches.ReplaceAsync(watch, ct);
        await _events.WatchUpdatedAsync(watch, ct);
    }

    private async Task PruneAsync(string userId, string keepProbeId, DateTime now, CancellationToken ct)
    {
        var active = await _probes.GetActiveByUserAsync(userId, now, ct);
        if (active.Count <= MaxActiveProbesPerUser) return;

        foreach (var stale in active.Where(p => p.Id != keepProbeId).Skip(MaxActiveProbesPerUser - 1))
        {
            await EndTestModeAsync(stale.WatchId, ct);
            await _probes.DeleteAsync(stale.Id, ct);
        }
    }

    /// <summary>Fusiona las solicitudes de prueba con los check-ins reales de la vigilancia (si la hay).</summary>
    private async Task<PingTestState> BuildStateAsync(PingProbe probe, Watch? watch, DateTime now, CancellationToken ct)
    {
        var activity = probe.Hits
            .Select(h => new PingActivity(h.ReceivedAtUtc, h.Kind, "Prueba", false, h.Method, h.RemoteIp, h.UserAgent, h.Payload))
            .ToList();

        if (probe.WatchId is not null)
        {
            var real = await _checkIns.GetRecentByWatchAsync(probe.WatchId, MaxActivityItems, ct);
            activity.AddRange(real.Select(c => new PingActivity(c.ReceivedAtUtc, c.Kind, c.Source.ToString(), true, null, c.RemoteIp, null, c.Payload)));
        }

        return new PingTestState(
            probe.Id,
            probe.Token,
            BuildPingUrl(probe.Token),
            probe.Mode,
            probe.ExpiresAtUtc,
            watch is not null && watch.IsTestMode(now) ? watch.TestModeUntilUtc : null,
            probe.LastHitAtUtc,
            probe.HitCount,
            activity.OrderByDescending(a => a.ReceivedAtUtc).Take(MaxActivityItems).ToList());
    }
}
