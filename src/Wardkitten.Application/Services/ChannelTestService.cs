// Feature: F05.06 — envío de prueba por un canal
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

/// <summary>
/// «Enviar prueba» (F05.06): manda un mensaje de prueba por un binding, con el mismo canal
/// (<see cref="INotificationChannel"/>) y la misma resolución de destino que usarán las alertas, para que el
/// usuario compruebe que le llegan antes de depender de ellas. Los canales de pago no se prueban: costarían
/// créditos.
/// </summary>
public sealed class ChannelTestService
{
    private readonly IReadOnlyDictionary<ChannelType, INotificationChannel> _channels;
    private readonly IUserRepository _users;
    private readonly INotificationLogRepository _logs;
    private readonly IClock _clock;

    public ChannelTestService(IEnumerable<INotificationChannel> channels, IUserRepository users,
        INotificationLogRepository logs, IClock clock)
    {
        _channels = channels.ToDictionary(c => c.Channel);
        _users = users;
        _logs = logs;
        _clock = clock;
    }

    /// <summary>Devuelve el mensaje a enseñar al usuario (éxito) o el motivo del fallo.</summary>
    public async Task<Result<string>> SendTestAsync(string userId, ChannelBinding binding, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<string>.Fail("Usuario no encontrado.");

        var name = ChannelDestinations.DisplayName(binding.ChannelType);
        if (binding.ChannelType.IsMetered())
            return Result<string>.Fail($"Las pruebas por {name} no se envían porque consumen créditos.");

        var valid = ChannelBindingRules.Validate(binding, user);
        if (!valid.Success) return Result<string>.Fail(valid.Error!);

        if (!_channels.TryGetValue(binding.ChannelType, out var channel))
            return Result<string>.Fail($"El canal {name} no está disponible ahora mismo.");

        var destination = ChannelDestinations.Resolve(binding, user)!.Trim();
        NotificationResult sent;
        try
        {
            sent = await channel.SendAsync(new NotificationMessage
            {
                Channel = binding.ChannelType,
                Destination = destination,
                Title = "🐾 Wardkitten · mensaje de prueba",
                Body = $"Si lees esto, las alertas de Wardkitten por {name} te llegarán correctamente.",
                Severity = Severity.Low,
            }, ct);
        }
        catch (Exception ex)
        {
            sent = NotificationResult.Fail(ex.Message);
        }

        await _logs.InsertAsync(new NotificationLog
        {
            UserId = user.Id,
            Channel = binding.ChannelType,
            Destination = destination,
            Kind = "test",
            Success = sent.Success,
            ProviderMessageId = sent.ProviderMessageId,
            Error = sent.Error,
            SentAtUtc = _clock.UtcNow,
        }, ct);

        return sent.Success
            ? Result<string>.Ok($"Prueba enviada por {name}. Comprueba que te ha llegado.")
            : Result<string>.Fail($"No se pudo enviar la prueba por {name}: {sent.Error}");
    }
}
