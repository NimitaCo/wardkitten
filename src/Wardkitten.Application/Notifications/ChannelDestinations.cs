using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Notifications;

/// <summary>
/// Resolución del destino de un binding: el <see cref="ChannelBinding.DestinationOverride"/> si lo hay o, si
/// no, el destino por defecto del usuario para ese canal. Compartido por el dispatcher de alertas (F05.01)
/// y por los envíos de prueba (F05.06), para que la prueba llegue exactamente adonde llegará la alerta.
/// </summary>
public static class ChannelDestinations
{
    public static string? Resolve(ChannelBinding binding, User user)
    {
        if (!string.IsNullOrWhiteSpace(binding.DestinationOverride))
            return binding.DestinationOverride;

        return binding.ChannelType switch
        {
            ChannelType.Email => user.Email,
            ChannelType.Telegram => user.TelegramChatId,
            ChannelType.Push => user.PushTokens.FirstOrDefault(),
            ChannelType.Sms => user.Phone,
            ChannelType.WhatsApp => user.Phone,
            _ => null,
        };
    }

    /// <summary>Nombre legible del canal para mensajes al usuario.</summary>
    public static string DisplayName(ChannelType type) => type switch
    {
        ChannelType.Email => "Email",
        ChannelType.Telegram => "Telegram",
        ChannelType.Push => "Push",
        ChannelType.Sms => "SMS",
        ChannelType.WhatsApp => "WhatsApp",
        ChannelType.Webhook => "Webhook",
        ChannelType.Slack => "Slack",
        ChannelType.Discord => "Discord",
        ChannelType.MicrosoftTeams => "Microsoft Teams",
        _ => type.ToString(),
    };
}
