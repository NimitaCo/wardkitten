using Wardkitten.Shared.UI.Services;

namespace Wardkitten.Mobile.Services;

/// <summary>
/// Registra el token FCM del dispositivo en la API para recibir push. La obtención del token es
/// específica de plataforma (Firebase SDK en Android/iOS) y queda como TODO de integración. Feature: F09.
/// </summary>
public sealed class FcmTokenRegistrar
{
    private readonly WardkittenApiClient _api;

    public FcmTokenRegistrar(WardkittenApiClient api) => _api = api;

    public async Task RegisterAsync()
    {
        var token = await GetDeviceTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            await _api.RegisterPushTokenAsync(token);
    }

    // TODO(F09): integrar Firebase Cloud Messaging por plataforma (Plugin.Firebase o SDK nativo)
    // y devolver el token real del dispositivo. De momento devuelve null (sin push).
    private static Task<string?> GetDeviceTokenAsync() => Task.FromResult<string?>(null);
}
