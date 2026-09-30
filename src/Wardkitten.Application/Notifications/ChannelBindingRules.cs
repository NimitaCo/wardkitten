// Feature: F02.05 — canales por defecto del usuario (validación de bindings)
using System.Net;
using System.Net.Sockets;
using Wardkitten.Application.Common;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Notifications;

/// <summary>
/// Reglas para aceptar un canal como destino de avisos del usuario: un canal solo se guarda si de verdad
/// puede entregar. SMS/WhatsApp exigen teléfono verificado (SECURITY.md §2), Telegram una cuenta vinculada,
/// Push un dispositivo registrado y las integraciones salientes una URL <c>https</c> pública.
/// </summary>
public static class ChannelBindingRules
{
    public static Result Validate(ChannelBinding binding, User user)
    {
        switch (binding.ChannelType)
        {
            case ChannelType.Email:
                return Result.Ok();
            case ChannelType.Sms:
            case ChannelType.WhatsApp:
                return user.PhoneVerified && !string.IsNullOrWhiteSpace(user.Phone)
                    ? Result.Ok()
                    : Result.Fail($"Para avisarte por {ChannelDestinations.DisplayName(binding.ChannelType)} verifica antes tu teléfono.");
            case ChannelType.Telegram:
                return user.TelegramLinked
                    ? Result.Ok()
                    : Result.Fail("Vincula antes tu cuenta de Telegram.");
            case ChannelType.Push:
                return user.PushTokens.Count > 0
                    ? Result.Ok()
                    : Result.Fail("Instala la app móvil y activa las notificaciones para avisarte por Push.");
            case ChannelType.Webhook:
            case ChannelType.Slack:
            case ChannelType.Discord:
            case ChannelType.MicrosoftTeams:
                return IsPublicHttpsUrl(binding.DestinationOverride)
                    ? Result.Ok()
                    : Result.Fail($"La URL de {ChannelDestinations.DisplayName(binding.ChannelType)} debe ser una dirección https:// válida.");
            default:
                return Result.Fail("Canal no soportado.");
        }
    }

    /// <summary>
    /// URL absoluta <c>https</c> que no apunta a la propia máquina ni a redes privadas (evita usar los envíos
    /// de prueba para sondear la red interna del clúster). Los nombres DNS no se resuelven aquí.
    /// </summary>
    public static bool IsPublicHttpsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host)) return false;
        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return false;
        return !IPAddress.TryParse(uri.Host.Trim('[', ']'), out var ip) || !IsPrivate(ip);
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return true;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv4MappedToIPv6) return IsPrivate(ip.MapToIPv4());
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;
        }

        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254)
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)   // CGNAT (redes internas de algunos clústeres)
            || b[0] == 0;
    }
}
