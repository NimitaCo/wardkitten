using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wardkitten.Application.Billing;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.Security;
using Wardkitten.Infrastructure.Billing;
using Wardkitten.Infrastructure.Notifications;
using Wardkitten.Infrastructure.Security;

namespace Wardkitten.Infrastructure.DependencyInjection;

public static class IntegrationsRegistration
{
    /// <summary>
    /// Registra las integraciones externas: seguridad (BCrypt/JWT/magic links), canales de notificación
    /// (Email/Telegram/Push/SMS/WhatsApp) y Stripe. Opciones por variables de entorno (ver SECURITY.md).
    /// </summary>
    public static IServiceCollection AddWardkittenIntegrations(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient();

        // ---- Seguridad ----
        services.Configure<JwtOptions>(o =>
        {
            o.Secret = config["JWT_SECRET"] ?? "wardkitten-dev-insecure-secret-change-me-please-0123456789";
            o.Issuer = config["JWT_ISSUER"] ?? "wardkitten";
            o.Audience = config["JWT_AUDIENCE"] ?? "wardkitten";
            o.AccessTokenMinutes = ParseInt(config["JWT_ACCESS_MINUTES"], 60);
        });
        services.Configure<MagicLinkOptions>(o =>
        {
            o.Secret = config["MAGICLINK_SECRET"] ?? "wardkitten-dev-magiclink-secret-change-me-0123456789";
            o.TtlMinutes = ParseInt(config["MAGICLINK_TTL_MINUTES"], 60 * 24);
        });
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<HmacMagicLinkService>();
        services.AddSingleton<IAckLinkBuilder>(sp => sp.GetRequiredService<HmacMagicLinkService>());
        services.AddSingleton<IMagicLinkValidator>(sp => sp.GetRequiredService<HmacMagicLinkService>());

        // ---- Canales ----
        services.Configure<EmailOptions>(o =>
        {
            o.Host = config["SMTP_HOST"] ?? string.Empty;
            o.Port = ParseInt(config["SMTP_PORT"], 587);
            o.User = config["SMTP_USER"] ?? string.Empty;
            o.Password = config["SMTP_PASSWORD"] ?? string.Empty;
            o.FromAddress = config["SMTP_FROM"] ?? "noreply@wardkitten.com";
            o.FromName = config["SMTP_FROM_NAME"] ?? "Wardkitten";
        });
        services.Configure<TelegramOptions>(o => o.BotToken = config["TELEGRAM_BOT_TOKEN"] ?? string.Empty);
        services.Configure<PushOptions>(o => o.ServiceAccountJson = config["FCM_SERVICE_ACCOUNT_JSON"]);
        services.Configure<TwilioOptions>(o =>
        {
            o.AccountSid = config["TWILIO_ACCOUNT_SID"] ?? string.Empty;
            o.AuthToken = config["TWILIO_AUTH_TOKEN"] ?? string.Empty;
            o.SmsFrom = config["TWILIO_SMS_FROM"] ?? string.Empty;
            o.WhatsAppFrom = config["TWILIO_WHATSAPP_FROM"] ?? string.Empty;
        });

        services.AddSingleton<INotificationChannel, EmailChannel>();
        services.AddSingleton<INotificationChannel, TelegramChannel>();
        services.AddSingleton<INotificationChannel, PushChannel>();
        services.AddSingleton<INotificationChannel, TwilioSmsChannel>();
        services.AddSingleton<INotificationChannel, TwilioWhatsAppChannel>();
        services.AddSingleton<INotificationChannel, WebhookChannel>();
        services.AddSingleton<INotificationChannel, SlackChannel>();
        services.AddSingleton<INotificationChannel, DiscordChannel>();

        // ---- Pagos (Stripe) ----
        services.Configure<StripeOptions>(o =>
        {
            o.SecretKey = config["STRIPE_SECRET_KEY"] ?? string.Empty;
            o.WebhookSecret = config["STRIPE_WEBHOOK_SECRET"] ?? string.Empty;
            o.PriceProMonthly = config["STRIPE_PRICE_PRO"] ?? string.Empty;
            o.PriceTeamMonthly = config["STRIPE_PRICE_TEAM"] ?? string.Empty;
            o.CreditUnitAmountCents = ParseInt(config["STRIPE_CREDIT_CENTS"], 100);
            o.CreditCurrency = config["STRIPE_CREDIT_CURRENCY"] ?? "eur";
        });
        services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
        services.AddSingleton<StripeWebhookProcessor>();

        return services;
    }

    private static int ParseInt(string? value, int fallback)
        => int.TryParse(value, out var n) ? n : fallback;
}
