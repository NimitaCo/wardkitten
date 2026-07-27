using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Abstractions.Persistence;

public interface IWatchRepository : IRepository<Watch>
{
    Task<IReadOnlyList<Watch>> GetByUserAsync(string userId, CancellationToken ct = default);
    Task<Watch?> GetByPingTokenAsync(string pingToken, CancellationToken ct = default);
    Task<long> CountByUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Watches activos cuyo deadline (vencimiento + gracia) ya pasó. Streaming para no cargar en memoria
    /// colecciones grandes (ver AGENTS.md). Lo consume el motor de evaluación.
    /// </summary>
    IAsyncEnumerable<Watch> StreamDueAsync(DateTime nowUtc, CancellationToken ct = default);
}

public interface ICheckInRepository
{
    Task InsertAsync(CheckIn checkIn, CancellationToken ct = default);
    Task<IReadOnlyList<CheckIn>> GetRecentByWatchAsync(string watchId, int limit, CancellationToken ct = default);
}

/// <summary>
/// Bancos de pruebas de URLs de ping (F03.03). La colección tiene índice TTL sobre <c>expiresAtUtc</c>:
/// los borradores abandonados desaparecen solos, sin barridos ni trabajos programados.
/// </summary>
public interface IPingProbeRepository : IRepository<PingProbe>
{
    Task<PingProbe?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<PingProbe?> GetByWatchAsync(string watchId, CancellationToken ct = default);

    /// <summary>Bancos vivos del usuario, del más reciente al más antiguo (sirve para acotar cuántos abre).</summary>
    Task<IReadOnlyList<PingProbe>> GetActiveByUserAsync(string userId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Anota una solicitud de forma atómica (push acotado a <see cref="PingProbe.MaxHits"/> + contadores),
    /// para no perder pings simultáneos. Devuelve false si el banco ya había caducado o no existe.
    /// </summary>
    Task<bool> RegisterHitAsync(string probeId, PingProbeHit hit, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>Renueva la caducidad mientras la pantalla de alta/edición siga abierta (TTL deslizante).</summary>
    Task ExtendAsync(string probeId, DateTime expiresAtUtc, CancellationToken ct = default);

    Task DeleteByWatchAsync(string watchId, CancellationToken ct = default);
}

public interface IIncidentRepository : IRepository<Incident>
{
    Task<Incident?> GetOpenByWatchAsync(string watchId, CancellationToken ct = default);
    Task<IReadOnlyList<Incident>> GetByUserAsync(string userId, int skip, int take, CancellationToken ct = default);
    IAsyncEnumerable<Incident> StreamOpenAsync(CancellationToken ct = default);

    /// <summary>
    /// Inserta el incidente candidato; si ya existe uno abierto para el watch (índice parcial único),
    /// devuelve el existente. Garantiza un único incidente abierto por watch sin acoplar Application a Mongo.
    /// </summary>
    Task<Incident> OpenOrGetExistingAsync(Incident candidate, CancellationToken ct = default);
}

public interface IEscalationPolicyRepository : IRepository<EscalationPolicy>
{
    Task<IReadOnlyList<EscalationPolicy>> GetByUserAsync(string userId, CancellationToken ct = default);
}
