using Microsoft.Extensions.DependencyInjection;
using Wardkitten.Application.Evaluation;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.RealTime;
using Wardkitten.Application.Services;

namespace Wardkitten.Application.DependencyInjection;

public static class ApplicationRegistration
{
    /// <summary>
    /// Registra los servicios de aplicación. Los canales de notificación, la pasarela de pago, el hasher,
    /// el servicio de tokens y (opcionalmente) el publicador en tiempo real se registran en Infrastructure/Api.
    /// </summary>
    public static IServiceCollection AddWardkittenApplication(this IServiceCollection services, string? publicBaseUrl = null)
    {
        services.Configure<NotificationOptions>(o =>
        {
            if (!string.IsNullOrWhiteSpace(publicBaseUrl)) o.PublicBaseUrl = publicBaseUrl!;
        });

        // Por defecto (sustituibles por la API): magic links sin firma y publicador no-op.
        services.AddSingleton<IAckLinkBuilder, DefaultAckLinkBuilder>();
        services.AddSingleton<IWatchEventPublisher, NoopWatchEventPublisher>();

        services.AddSingleton<WalletService>();
        services.AddSingleton<WatchService>();
        services.AddSingleton<IncidentService>();
        services.AddSingleton<CheckInService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<BillingService>();
        services.AddSingleton<NotificationDispatcher>();
        services.AddSingleton<INotificationDispatcher>(sp => sp.GetRequiredService<NotificationDispatcher>());
        services.AddSingleton<EvaluationEngine>();

        return services;
    }
}
