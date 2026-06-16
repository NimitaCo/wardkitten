using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Security;
using Wardkitten.Domain.Identity;

namespace Wardkitten.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "wardkitten";
    public string Audience { get; set; } = "wardkitten";
    public int AccessTokenMinutes { get; set; } = 60;
}

/// <summary>Emite JWT de acceso firmados (HS256) y refresh tokens hasheados (SHA-256). Ver SECURITY.md §2.</summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessToken CreateAccessToken(User user)
    {
        var expires = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.DisplayName),
            new("plan", user.Plan.ToString()),
            new("tz", user.TimeZoneId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _clock.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string GenerateRefreshToken() => SecureTokenGenerator.New(32);

    public string HashRefreshToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
