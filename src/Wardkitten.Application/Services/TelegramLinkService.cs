// Feature: F05.05 — vinculación de Telegram por deep link
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.Security;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

/// <summary>Configuración del bot de Telegram para la vinculación (variables de entorno, ver IntegrationsRegistration).</summary>
public sealed class TelegramLinkOptions
{
    /// <summary>Usuario del bot sin «@» (<c>TELEGRAM_BOT_USERNAME</c>). Vacío = vinculación deshabilitada.</summary>
    public string BotUsername { get; set; } = string.Empty;

    /// <summary>
    /// Secreto que Telegram envía en <c>X-Telegram-Bot-Api-Secret-Token</c> (<c>TELEGRAM_WEBHOOK_SECRET</c>,
    /// el mismo que se pasa a <c>setWebhook</c>). Vacío = el webhook rechaza todo.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
}

public sealed record TelegramLinkCode(string Code, string DeepLink, DateTime ExpiresAtUtc);

public enum TelegramUpdateOutcome
{
    /// <summary>No es un <c>/start</c>: no se responde.</summary>
    Ignored,
    /// <summary><c>/start</c> sin código: se responde con instrucciones.</summary>
    MissingCode,
    /// <summary>Código desconocido o caducado.</summary>
    InvalidCode,
    Linked,
}

/// <summary>
/// Vincula la cuenta con un chat de Telegram (F05.05). La web pide un código de un solo uso y abre
/// <c>https://t.me/&lt;bot&gt;?start=&lt;código&gt;</c>; al pulsar «Start», Telegram entrega <c>/start &lt;código&gt;</c> al
/// webhook y aquí se guarda el chat como destino por defecto del canal Telegram. El código se guarda hasheado
/// y caduca en 15 minutos, como los de verificación de email.
/// </summary>
public sealed class TelegramLinkService
{
    // HARDCODE (ver HARDCODED.md): vida del código de vinculación de Telegram.
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly TelegramLinkOptions _options;
    private readonly IClock _clock;

    public TelegramLinkService(IUserRepository users, ITokenService tokens, IEnumerable<INotificationChannel> channels,
        IOptions<TelegramLinkOptions> options, IClock clock)
    {
        _users = users;
        _tokens = tokens;
        _channels = channels;
        _options = options.Value;
        _clock = clock;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BotUsername);

    public async Task<Result<TelegramLinkCode>> CreateLinkCodeAsync(string userId, CancellationToken ct = default)
    {
        if (!IsConfigured) return Result<TelegramLinkCode>.Fail("Telegram no está disponible en este momento.");
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<TelegramLinkCode>.Fail("Usuario no encontrado.");

        var code = SecureTokenGenerator.LinkCode();
        var expires = _clock.UtcNow.Add(CodeLifetime);
        user.TelegramLinkCodeHash = _tokens.HashRefreshToken(code);
        user.TelegramLinkExpiresUtc = expires;
        await _users.ReplaceAsync(user, ct);

        var bot = _options.BotUsername.Trim().TrimStart('@');
        return Result<TelegramLinkCode>.Ok(new TelegramLinkCode(code, $"https://t.me/{bot}?start={code}", expires));
    }

    public async Task<bool> IsLinkedAsync(string userId, CancellationToken ct = default)
        => (await _users.GetByIdAsync(userId, ct))?.TelegramLinked == true;

    /// <summary>Desvincula el chat y quita Telegram de los canales por defecto (ya no podría entregar).</summary>
    public async Task<Result> UnlinkAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Fail("Usuario no encontrado.");

        user.TelegramChatId = null;
        user.TelegramLinkCodeHash = null;
        user.TelegramLinkExpiresUtc = null;
        user.DefaultChannelBindings = user.DefaultChannelBindings.Where(b => b.ChannelType != ChannelType.Telegram).ToList();
        await _users.ReplaceAsync(user, ct);
        return Result.Ok();
    }

    /// <summary>Comprueba en tiempo constante la cabecera secreta del webhook.</summary>
    public bool IsValidWebhookSecret(string? header)
    {
        if (string.IsNullOrEmpty(_options.WebhookSecret) || string.IsNullOrEmpty(header)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(header), Encoding.UTF8.GetBytes(_options.WebhookSecret));
    }

    /// <summary>
    /// Procesa un mensaje entrante del bot. Solo atiende <c>/start</c> (con o sin <c>@bot</c>): con código válido
    /// vincula el chat; sin código o con uno inválido responde con instrucciones.
    /// </summary>
    public async Task<TelegramUpdateOutcome> HandleMessageAsync(long chatId, string? text, CancellationToken ct = default)
    {
        var parts = (text ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return TelegramUpdateOutcome.Ignored;
        var command = parts[0];
        if (!command.Equals("/start", StringComparison.OrdinalIgnoreCase)
            && !command.StartsWith("/start@", StringComparison.OrdinalIgnoreCase))
            return TelegramUpdateOutcome.Ignored;

        var chat = chatId.ToString(CultureInfo.InvariantCulture);
        if (parts.Length < 2)
        {
            await ReplyAsync(chat, "👋 Hola, soy Wardkitten",
                "Para recibir aquí tus alertas, entra en Wardkitten, ve a «Cómo avisarte» → Telegram y pulsa «Vincular».", ct);
            return TelegramUpdateOutcome.MissingCode;
        }

        var hash = _tokens.HashRefreshToken(parts[1].ToLowerInvariant());
        var user = await _users.GetByTelegramLinkCodeHashAsync(hash, ct);
        if (user is null || user.TelegramLinkExpiresUtc is null || user.TelegramLinkExpiresUtc < _clock.UtcNow)
        {
            await ReplyAsync(chat, "❌ No he podido vincular tu cuenta",
                "El código no es válido o ha caducado. Genera uno nuevo desde Wardkitten y vuelve a pulsar «Start».", ct);
            return TelegramUpdateOutcome.InvalidCode;
        }

        user.TelegramChatId = chat;
        user.TelegramLinkCodeHash = null;
        user.TelegramLinkExpiresUtc = null;
        await _users.ReplaceAsync(user, ct);

        await ReplyAsync(chat, "✅ Cuenta vinculada con Wardkitten",
            "Te avisaremos aquí cuando alguna de tus vigilancias no llegue a tiempo.", ct);
        return TelegramUpdateOutcome.Linked;
    }

    private async Task ReplyAsync(string chatId, string title, string body, CancellationToken ct)
    {
        var channel = _channels.FirstOrDefault(c => c.Channel == ChannelType.Telegram);
        if (channel is null) return;
        try
        {
            await channel.SendAsync(new NotificationMessage
            {
                Channel = ChannelType.Telegram, Destination = chatId, Title = title, Body = body,
            }, ct);
        }
        catch { /* best-effort: la vinculación ya está guardada */ }
    }
}
