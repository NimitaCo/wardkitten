using MongoDB.Driver;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Domain.Identity;

namespace Wardkitten.Infrastructure.Mongo.Repositories;

public sealed class UserRepository : MongoRepository<User>, IUserRepository
{
    public UserRepository(MongoContext ctx, IClock clock) : base(ctx.Users, clock) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await Collection.Find(Builders<User>.Filter.Eq(u => u.Email, email.Trim().ToLowerInvariant()))
                           .FirstOrDefaultAsync(ct);

    public async Task<User?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken ct = default)
        => await Collection.Find(Builders<User>.Filter.Eq(u => u.StripeCustomerId, stripeCustomerId))
                           .FirstOrDefaultAsync(ct);

    public async Task<User?> GetByTelegramLinkCodeHashAsync(string codeHash, CancellationToken ct = default)
        => await Collection.Find(Builders<User>.Filter.Eq(u => u.TelegramLinkCodeHash, codeHash))
                           .FirstOrDefaultAsync(ct);
}

public sealed class RefreshTokenRepository : MongoRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(MongoContext ctx, IClock clock) : base(ctx.RefreshTokens, clock) { }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => await Collection.Find(Builders<RefreshToken>.Filter.Eq(t => t.TokenHash, tokenHash))
                           .FirstOrDefaultAsync(ct);
}
