using Wardkitten.Domain.Identity;

namespace Wardkitten.Application.Abstractions.Persistence;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken ct = default);

    /// <summary>Usuario con ese código de vinculación de Telegram pendiente (hash). F05.05.</summary>
    Task<User?> GetByTelegramLinkCodeHashAsync(string codeHash, CancellationToken ct = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
}
