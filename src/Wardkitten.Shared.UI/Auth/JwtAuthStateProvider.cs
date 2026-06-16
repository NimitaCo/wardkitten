using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Wardkitten.Shared.UI.Auth;

/// <summary>Construye el estado de autenticación a partir del JWT almacenado (sin validar firma; solo lectura de claims).</summary>
public sealed class JwtAuthStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private readonly ITokenStore _store;

    public JwtAuthStateProvider(ITokenStore store) => _store = store;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _store.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token)) return Anonymous;

        var claims = ParseClaims(token).ToList();
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (long.TryParse(exp, out var expUnix) && DateTimeOffset.FromUnixTimeSeconds(expUnix) < DateTimeOffset.UtcNow)
            return Anonymous;

        var identity = new ClaimsIdentity(claims, "jwt", "name", ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) yield break;
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Base64UrlDecode(parts[1]));
        if (payload is null) yield break;

        foreach (var (key, value) in payload)
        {
            if (key == "role" && value.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in value.EnumerateArray())
                    yield return new Claim(ClaimTypes.Role, r.GetString() ?? string.Empty);
            }
            else
            {
                yield return new Claim(key, value.ToString());
            }
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }
}
