using Microsoft.Extensions.Logging;
using Wardkitten.Mobile.Services;
using Wardkitten.Shared.UI.DependencyInjection;

namespace Wardkitten.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // En móvil el token se guarda en SecureStorage (no localStorage).
        var apiBaseUrl = "https://api.wardkitten.com";
        builder.Services.AddWardkittenClient(apiBaseUrl, typeof(SecureStorageTokenStore));

        // Registro del token FCM al iniciar sesión (implementación por plataforma; ver F09).
        builder.Services.AddSingleton<FcmTokenRegistrar>();

        return builder.Build();
    }
}
