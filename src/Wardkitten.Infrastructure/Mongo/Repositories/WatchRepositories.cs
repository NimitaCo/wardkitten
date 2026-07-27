using System.Runtime.CompilerServices;
using MongoDB.Driver;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Infrastructure.Mongo.Repositories;

public sealed class WatchRepository : MongoRepository<Watch>, IWatchRepository
{
    public WatchRepository(MongoContext ctx, IClock clock) : base(ctx.Watches, clock) { }

    public async Task<IReadOnlyList<Watch>> GetByUserAsync(string userId, CancellationToken ct = default)
        => await Collection.Find(Builders<Watch>.Filter.Eq(w => w.UserId, userId))
                           .SortByDescending(w => w.CreatedAtUtc)
                           .ToListAsync(ct);

    public async Task<Watch?> GetByPingTokenAsync(string pingToken, CancellationToken ct = default)
        => await Collection.Find(Builders<Watch>.Filter.Eq(w => w.PingToken, pingToken))
                           .FirstOrDefaultAsync(ct);

    public async Task<long> CountByUserAsync(string userId, CancellationToken ct = default)
        => await Collection.CountDocumentsAsync(Builders<Watch>.Filter.Eq(w => w.UserId, userId), cancellationToken: ct);

    public async IAsyncEnumerable<Watch> StreamDueAsync(DateTime nowUtc, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Candidatos: no pausados con vencimiento ya alcanzado. El evaluador aplica la gracia exacta
        // en memoria (la aritmética nextDue+grace por watch no se hace en el servidor).
        var filter = Builders<Watch>.Filter.And(
            Builders<Watch>.Filter.Eq(w => w.Paused, false),
            Builders<Watch>.Filter.Ne(w => w.NextDueAtUtc, null),
            Builders<Watch>.Filter.Lte(w => w.NextDueAtUtc, nowUtc));

        using var cursor = await Collection.FindAsync(filter, cancellationToken: ct);
        while (await cursor.MoveNextAsync(ct))
            foreach (var watch in cursor.Current)
                yield return watch;
    }
}

public sealed class CheckInRepository : ICheckInRepository
{
    private readonly IMongoCollection<CheckIn> _collection;
    private readonly IClock _clock;

    public CheckInRepository(MongoContext ctx, IClock clock)
    {
        _collection = ctx.CheckIns;
        _clock = clock;
    }

    public async Task InsertAsync(CheckIn checkIn, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(checkIn.Id))
            checkIn.Id = Guid.NewGuid().ToString("N");
        if (checkIn.ReceivedAtUtc == default)
            checkIn.ReceivedAtUtc = _clock.UtcNow;
        checkIn.CreatedAtUtc = checkIn.ReceivedAtUtc;
        checkIn.UpdatedAtUtc = checkIn.ReceivedAtUtc;
        await _collection.InsertOneAsync(checkIn, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<CheckIn>> GetRecentByWatchAsync(string watchId, int limit, CancellationToken ct = default)
        => await _collection.Find(Builders<CheckIn>.Filter.Eq(c => c.WatchId, watchId))
                            .SortByDescending(c => c.ReceivedAtUtc)
                            .Limit(limit)
                            .ToListAsync(ct);
}

/// <summary>
/// Bancos de pruebas de URLs de ping (F03.03). El auto-borrado lo hace el índice TTL sobre
/// <c>expiresAtUtc</c> (ver <see cref="MongoContext"/>); las consultas filtran también por caducidad
/// porque el barrido TTL de Mongo solo corre cada ~60 s.
/// </summary>
public sealed class PingProbeRepository : MongoRepository<PingProbe>, IPingProbeRepository
{
    public PingProbeRepository(MongoContext ctx, IClock clock) : base(ctx.PingProbes, clock) { }

    public async Task<PingProbe?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await Collection.Find(Builders<PingProbe>.Filter.Eq(p => p.Token, token)).FirstOrDefaultAsync(ct);

    public async Task<PingProbe?> GetByWatchAsync(string watchId, CancellationToken ct = default)
        => await Collection.Find(Builders<PingProbe>.Filter.Eq(p => p.WatchId, watchId))
                           .SortByDescending(p => p.CreatedAtUtc)
                           .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PingProbe>> GetActiveByUserAsync(string userId, DateTime nowUtc, CancellationToken ct = default)
        => await Collection.Find(Builders<PingProbe>.Filter.And(
                               Builders<PingProbe>.Filter.Eq(p => p.UserId, userId),
                               Builders<PingProbe>.Filter.Gt(p => p.ExpiresAtUtc, nowUtc)))
                           .SortByDescending(p => p.CreatedAtUtc)
                           .ToListAsync(ct);

    public async Task<bool> RegisterHitAsync(string probeId, PingProbeHit hit, DateTime nowUtc, CancellationToken ct = default)
    {
        // Push acotado + contadores en una sola operación atómica: dos pings simultáneos no se pisan.
        var filter = Builders<PingProbe>.Filter.And(
            ById(probeId),
            Builders<PingProbe>.Filter.Gt(p => p.ExpiresAtUtc, nowUtc));

        var update = Builders<PingProbe>.Update
            .PushEach(p => p.Hits, new[] { hit }, slice: -PingProbe.MaxHits)
            .Inc(p => p.HitCount, 1)
            .Set(p => p.LastHitAtUtc, (DateTime?)hit.ReceivedAtUtc)
            .Set(p => p.UpdatedAtUtc, nowUtc);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task ExtendAsync(string probeId, DateTime expiresAtUtc, CancellationToken ct = default)
        => await Collection.UpdateOneAsync(
            ById(probeId),
            Builders<PingProbe>.Update.Set(p => p.ExpiresAtUtc, expiresAtUtc).Set(p => p.UpdatedAtUtc, Clock.UtcNow),
            cancellationToken: ct);

    public async Task DeleteByWatchAsync(string watchId, CancellationToken ct = default)
        => await Collection.DeleteManyAsync(Builders<PingProbe>.Filter.Eq(p => p.WatchId, watchId), ct);
}

public sealed class IncidentRepository : MongoRepository<Incident>, IIncidentRepository
{
    public IncidentRepository(MongoContext ctx, IClock clock) : base(ctx.Incidents, clock) { }

    public async Task<Incident?> GetOpenByWatchAsync(string watchId, CancellationToken ct = default)
        => await Collection.Find(Builders<Incident>.Filter.And(
                Builders<Incident>.Filter.Eq(i => i.WatchId, watchId),
                Builders<Incident>.Filter.Eq(i => i.State, IncidentState.Open)))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Incident>> GetByUserAsync(string userId, int skip, int take, CancellationToken ct = default)
        => await Collection.Find(Builders<Incident>.Filter.Eq(i => i.UserId, userId))
                           .SortByDescending(i => i.OpenedAtUtc)
                           .Skip(skip).Limit(take)
                           .ToListAsync(ct);

    public async IAsyncEnumerable<Incident> StreamOpenAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var filter = Builders<Incident>.Filter.Eq(i => i.State, IncidentState.Open);
        using var cursor = await Collection.FindAsync(filter, cancellationToken: ct);
        while (await cursor.MoveNextAsync(ct))
            foreach (var incident in cursor.Current)
                yield return incident;
    }

    public async Task<Incident> OpenOrGetExistingAsync(Incident candidate, CancellationToken ct = default)
    {
        try
        {
            await InsertAsync(candidate, ct);
            return candidate;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return (await GetOpenByWatchAsync(candidate.WatchId, ct))!;
        }
    }
}

public sealed class EscalationPolicyRepository : MongoRepository<EscalationPolicy>, IEscalationPolicyRepository
{
    public EscalationPolicyRepository(MongoContext ctx, IClock clock) : base(ctx.EscalationPolicies, clock) { }

    public async Task<IReadOnlyList<EscalationPolicy>> GetByUserAsync(string userId, CancellationToken ct = default)
        => await Collection.Find(Builders<EscalationPolicy>.Filter.Eq(p => p.UserId, userId)).ToListAsync(ct);
}
