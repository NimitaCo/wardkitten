using Wardkitten.Shared.Contracts;
using Wardkitten.Shared.UI.Services;

namespace Wardkitten.Shared.UI.Auth;

/// <summary>Login/registro/logout del cliente: persiste tokens y notifica el cambio de estado de auth.</summary>
public sealed class ClientAuthService
{
    private readonly WardkittenApiClient _api;
    private readonly ITokenStore _store;
    private readonly JwtAuthStateProvider _authState;

    public ClientAuthService(WardkittenApiClient api, ITokenStore store, JwtAuthStateProvider authState)
    {
        _api = api;
        _store = store;
        _authState = authState;
    }

    public async Task<ApiResult<UserDto>> LoginAsync(string email, string password)
    {
        var result = await _api.LoginAsync(new LoginRequest(email, password));
        return await PersistAsync(result);
    }

    public async Task<ApiResult<UserDto>> RegisterAsync(string email, string password, string displayName, string? timeZoneId)
    {
        var result = await _api.RegisterAsync(new RegisterRequest(email, password, displayName, timeZoneId, "es"));
        return await PersistAsync(result);
    }

    public async Task LogoutAsync()
    {
        await _store.ClearAsync();
        _authState.NotifyChanged();
    }

    private async Task<ApiResult<UserDto>> PersistAsync(ApiResult<AuthResponse> result)
    {
        if (!result.Ok || result.Value is null)
            return ApiResult<UserDto>.Failure(result.Error ?? "Error de autenticación.");

        await _store.SetAsync(result.Value.AccessToken, result.Value.RefreshToken);
        _authState.NotifyChanged();
        return ApiResult<UserDto>.Success(result.Value.User);
    }
}
