using Microsoft.Maui.Storage;
using Wardkitten.Shared.UI.Auth;

namespace Wardkitten.Mobile.Services;

/// <summary>Almacén de tokens para móvil usando SecureStorage (cifrado del sistema), en lugar de localStorage.</summary>
public sealed class SecureStorageTokenStore : ITokenStore
{
    private const string AccessKey = "wk_access";
    private const string RefreshKey = "wk_refresh";

    public async ValueTask<string?> GetAccessTokenAsync() => await SecureStorage.Default.GetAsync(AccessKey);
    public async ValueTask<string?> GetRefreshTokenAsync() => await SecureStorage.Default.GetAsync(RefreshKey);

    public async ValueTask SetAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(AccessKey, accessToken);
        await SecureStorage.Default.SetAsync(RefreshKey, refreshToken);
    }

    public ValueTask ClearAsync()
    {
        SecureStorage.Default.Remove(AccessKey);
        SecureStorage.Default.Remove(RefreshKey);
        return ValueTask.CompletedTask;
    }
}
