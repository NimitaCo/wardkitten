using System.Security.Cryptography;
using Wardkitten.Domain.Identity;

namespace Wardkitten.Application.Security;

/// <summary>Hash de contraseñas (BCrypt en infraestructura). Ver SECURITY.md.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Emisión y hashing de tokens JWT y refresh tokens.</summary>
public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}

/// <summary>
/// Genera tokens secretos e inadivinables para URLs de ping y magic links (128 bits, ver SECURITY.md).
/// Es determinista en cuanto a formato (hex) y no requiere infraestructura.
/// </summary>
public static class SecureTokenGenerator
{
    public static string New(int bytes = 16)
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();

    /// <summary>Código numérico de N dígitos para OTP (verificación de teléfono/email).</summary>
    public static string NumericCode(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        var n = RandomNumberGenerator.GetInt32(0, max);
        return n.ToString(new string('0', digits));
    }
}
