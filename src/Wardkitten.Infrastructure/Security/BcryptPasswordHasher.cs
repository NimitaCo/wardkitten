using Wardkitten.Application.Security;

namespace Wardkitten.Infrastructure.Security;

/// <summary>Hash de contraseñas con BCrypt (work factor 12). Ver SECURITY.md §2.</summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}
