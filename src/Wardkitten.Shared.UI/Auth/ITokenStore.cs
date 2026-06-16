using Microsoft.JSInterop;

namespace Wardkitten.Shared.UI.Auth;

/// <summary>Almacena los tokens en el cliente (localStorage en web; sustituible en móvil).</summary>
public interface ITokenStore
{
    ValueTask<string?> GetAccessTokenAsync();
    ValueTask<string?> GetRefreshTokenAsync();
    ValueTask SetAsync(string accessToken, string refreshToken);
    ValueTask ClearAsync();
}

public sealed class LocalStorageTokenStore : ITokenStore
{
    private const string AccessKey = "wk_access";
    private const string RefreshKey = "wk_refresh";
    private readonly IJSRuntime _js;

    public LocalStorageTokenStore(IJSRuntime js) => _js = js;

    public async ValueTask<string?> GetAccessTokenAsync() => await GetAsync(AccessKey);
    public async ValueTask<string?> GetRefreshTokenAsync() => await GetAsync(RefreshKey);

    public async ValueTask SetAsync(string accessToken, string refreshToken)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", AccessKey, accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
    }

    public async ValueTask ClearAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", AccessKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
    }

    private async ValueTask<string?> GetAsync(string key)
    {
        var value = await _js.InvokeAsync<string?>("localStorage.getItem", key);
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
