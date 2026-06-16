using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Wardkitten.Shared.UI.Auth;
using Wardkitten.Shared.UI.Services;

namespace Wardkitten.Shared.UI.DependencyInjection;

public static class ClientServiceCollectionExtensions
{
    /// <summary>
    /// Registra el cliente de Wardkitten (token store, estado de auth, handler bearer, API tipada).
    /// Reutilizable por la web (Blazor WASM) y la app móvil (MAUI). <paramref name="tokenStore"/> permite
    /// inyectar un almacén distinto en móvil (Secure Storage) en lugar de localStorage.
    /// </summary>
    public static IServiceCollection AddWardkittenClient(this IServiceCollection services, string apiBaseUrl, Type? tokenStore = null)
    {
        services.AddAuthorizationCore();

        if (tokenStore is null)
            services.AddScoped<ITokenStore, LocalStorageTokenStore>();
        else
            services.AddScoped(typeof(ITokenStore), tokenStore);

        services.AddScoped<JwtAuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());

        services.AddTransient<BearerHandler>();
        services.AddHttpClient<WardkittenApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<BearerHandler>();

        services.AddScoped<ClientAuthService>();
        return services;
    }
}
