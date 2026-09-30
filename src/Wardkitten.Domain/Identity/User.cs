using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Common;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Domain.Identity;

/// <summary>Cuenta de usuario. Feature: F01.01.</summary>
public sealed class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Zona horaria IANA del usuario; base para calcular deadlines de sus watches.</summary>
    public string TimeZoneId { get; set; } = "Europe/Madrid";
    public string Locale { get; set; } = "es";

    public List<string> Roles { get; set; } = new() { Identity.Roles.User };

    /// <summary>Teléfono en formato E.164 (para SMS/WhatsApp). Requiere verificación por OTP.</summary>
    public string? Phone { get; set; }

    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }

    public Plan Plan { get; set; } = Plan.Free;

    // Destinos por defecto de canales (se pueden sobreescribir por watch en cada ChannelBinding).
    public string? TelegramChatId { get; set; }
    public List<string> PushTokens { get; set; } = new();

    public string? StripeCustomerId { get; set; }
    public bool IsActive { get; set; } = true;

    // Códigos de verificación (hasheados, con expiración). El OTP de teléfono habilita SMS/WhatsApp.
    public string? EmailVerificationCodeHash { get; set; }
    public DateTime? EmailVerificationExpiresUtc { get; set; }
    public string? PhoneOtpHash { get; set; }
    public DateTime? PhoneOtpExpiresUtc { get; set; }

    // Vinculación de Telegram (F05.05): código de un solo uso (hasheado) que el usuario envía al bot con
    // /start <código>. Al recibirlo por el webhook se guarda el chat en TelegramChatId y se borra.
    public string? TelegramLinkCodeHash { get; set; }
    public DateTime? TelegramLinkExpiresUtc { get; set; }

    /// <summary>
    /// Canales por defecto (F02.05): los watches nuevos que no traen bindings propios nacen con una copia
    /// de estos. Vacío = solo Email. Se configuran en el asistente de bienvenida (F01.04).
    /// </summary>
    public List<ChannelBinding> DefaultChannelBindings { get; set; } = new();

    /// <summary>Cuándo terminó (o saltó) el asistente de bienvenida. Null = aún no lo ha cerrado (F01.04).</summary>
    public DateTime? OnboardingCompletedAtUtc { get; set; }

    public bool TelegramLinked => !string.IsNullOrWhiteSpace(TelegramChatId);

    public bool IsInRole(string role) => Roles.Contains(role);
}

public static class Roles
{
    public const string Admin = "admin";
    public const string User = "user";
}
