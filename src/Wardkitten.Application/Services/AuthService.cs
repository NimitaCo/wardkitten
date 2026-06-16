using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.Security;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Identity;

namespace Wardkitten.Application.Services;

public sealed record AuthResult(AccessToken Access, string RefreshToken, User User);

/// <summary>
/// Registro, login, rotación de refresh tokens y verificación de email/teléfono (OTP). El OTP de teléfono
/// es requisito para habilitar SMS/WhatsApp. Ver SECURITY.md. Feature: F01.
/// </summary>
public sealed class AuthService
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IWalletRepository _wallets;
    private readonly IPasswordHasher _passwords;
    private readonly ITokenService _tokens;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ISubscriptionRepository subscriptions,
        IWalletRepository wallets,
        IPasswordHasher passwords,
        ITokenService tokens,
        IClock clock)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _subscriptions = subscriptions;
        _wallets = wallets;
        _passwords = passwords;
        _tokens = tokens;
        _clock = clock;
    }

    public async Task<Result<AuthResult>> RegisterAsync(string email, string password, string displayName,
        string? timeZoneId, string? locale, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        if (!email.Contains('@')) return Result<AuthResult>.Fail("Email no válido.");
        if (password.Length < 8) return Result<AuthResult>.Fail("La contraseña debe tener al menos 8 caracteres.");
        if (await _users.GetByEmailAsync(email, ct) is not null)
            return Result<AuthResult>.Fail("Ya existe una cuenta con ese email.");

        var user = new User
        {
            Email = email,
            PasswordHash = _passwords.Hash(password),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName.Trim(),
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "Europe/Madrid" : timeZoneId!,
            Locale = string.IsNullOrWhiteSpace(locale) ? "es" : locale!,
            Plan = Plan.Free,
        };
        await _users.InsertAsync(user, ct);

        await _subscriptions.InsertAsync(new Subscription
        {
            UserId = user.Id, Plan = Plan.Free, Status = SubscriptionStatus.Active,
        }, ct);
        await _wallets.GetOrCreateForUserAsync(user.Id, ct);

        var auth = await IssueTokensAsync(user, null, ct);
        return Result<AuthResult>.Ok(auth);
    }

    public async Task<Result<AuthResult>> LoginAsync(string email, string password, string? ip, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null || !user.IsActive || !_passwords.Verify(password, user.PasswordHash))
            return Result<AuthResult>.Fail("Credenciales incorrectas.");

        var auth = await IssueTokensAsync(user, ip, ct);
        return Result<AuthResult>.Ok(auth);
    }

    public async Task<Result<AuthResult>> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, ct);
        if (stored is null || !stored.IsActive(_clock.UtcNow))
            return Result<AuthResult>.Fail("Refresh token inválido o expirado.");

        var user = await _users.GetByIdAsync(stored.UserId, ct);
        if (user is null || !user.IsActive)
            return Result<AuthResult>.Fail("Usuario no válido.");

        // Rotación: revoca el actual y emite uno nuevo.
        stored.RevokedAtUtc = _clock.UtcNow;
        var auth = await IssueTokensAsync(user, ip, ct);
        stored.ReplacedByTokenHash = _tokens.HashRefreshToken(auth.RefreshToken);
        await _refreshTokens.ReplaceAsync(stored, ct);
        return Result<AuthResult>.Ok(auth);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = _tokens.HashRefreshToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, ct);
        if (stored is null || stored.RevokedAtUtc is not null) return;
        stored.RevokedAtUtc = _clock.UtcNow;
        await _refreshTokens.ReplaceAsync(stored, ct);
    }

    // ---- Verificación de email / teléfono ----

    public async Task<Result<string>> GenerateEmailVerificationCodeAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<string>.Fail("Usuario no encontrado.");
        var code = SecureTokenGenerator.NumericCode();
        user.EmailVerificationCodeHash = _tokens.HashRefreshToken(code);
        user.EmailVerificationExpiresUtc = _clock.UtcNow.Add(CodeLifetime);
        await _users.ReplaceAsync(user, ct);
        return Result<string>.Ok(code); // el llamante lo envía por email
    }

    public async Task<Result> VerifyEmailAsync(string userId, string code, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Fail("Usuario no encontrado.");
        if (user.EmailVerificationCodeHash is null || user.EmailVerificationExpiresUtc < _clock.UtcNow)
            return Result.Fail("Código expirado, solicita uno nuevo.");
        if (user.EmailVerificationCodeHash != _tokens.HashRefreshToken(code))
            return Result.Fail("Código incorrecto.");

        user.EmailVerified = true;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationExpiresUtc = null;
        await _users.ReplaceAsync(user, ct);
        return Result.Ok();
    }

    public async Task<Result<string>> GeneratePhoneOtpAsync(string userId, string phoneE164, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<string>.Fail("Usuario no encontrado.");
        user.Phone = phoneE164.Trim();
        user.PhoneVerified = false;
        var code = SecureTokenGenerator.NumericCode();
        user.PhoneOtpHash = _tokens.HashRefreshToken(code);
        user.PhoneOtpExpiresUtc = _clock.UtcNow.Add(CodeLifetime);
        await _users.ReplaceAsync(user, ct);
        return Result<string>.Ok(code); // el llamante lo envía por SMS (coste sponsored, ver SECURITY.md)
    }

    public async Task<Result> VerifyPhoneAsync(string userId, string code, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Fail("Usuario no encontrado.");
        if (user.PhoneOtpHash is null || user.PhoneOtpExpiresUtc < _clock.UtcNow)
            return Result.Fail("Código expirado, solicita uno nuevo.");
        if (user.PhoneOtpHash != _tokens.HashRefreshToken(code))
            return Result.Fail("Código incorrecto.");

        user.PhoneVerified = true;
        user.PhoneOtpHash = null;
        user.PhoneOtpExpiresUtc = null;
        await _users.ReplaceAsync(user, ct);
        return Result.Ok();
    }

    private async Task<AuthResult> IssueTokensAsync(User user, string? ip, CancellationToken ct)
    {
        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.GenerateRefreshToken();
        await _refreshTokens.InsertAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokens.HashRefreshToken(refresh),
            ExpiresAtUtc = _clock.UtcNow.Add(RefreshLifetime),
            CreatedByIp = ip,
        }, ct);
        return new AuthResult(access, refresh, user);
    }
}
