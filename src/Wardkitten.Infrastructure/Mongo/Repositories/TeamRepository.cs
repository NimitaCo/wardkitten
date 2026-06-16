using MongoDB.Driver;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Domain.Teams;

namespace Wardkitten.Infrastructure.Mongo.Repositories;

public sealed class TeamRepository : MongoRepository<Team>, ITeamRepository
{
    public TeamRepository(MongoContext ctx, IClock clock) : base(ctx.Teams, clock) { }

    public async Task<IReadOnlyList<Team>> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        var filter = Builders<Team>.Filter.Or(
            Builders<Team>.Filter.Eq(t => t.OwnerId, userId),
            Builders<Team>.Filter.AnyEq(t => t.MemberUserIds, userId));
        return await Collection.Find(filter).SortByDescending(t => t.CreatedAtUtc).ToListAsync(ct);
    }
}
