using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Common;

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

    public bool IsInRole(string role) => Roles.Contains(role);
}

public static class Roles
{
    public const string Admin = "admin";
    public const string User = "user";
}
