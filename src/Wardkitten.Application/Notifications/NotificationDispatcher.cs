using Microsoft.Extensions.Logging;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Notifications;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Notifications;

/// <summary>
/// Entrega las alertas de un incidente por <b>todos los canales apilados</b> del watch que ya estén
/// "vencidos" según su retardo de escalado, respetando quiet-hours y saldo de wallet (metered), y
/// garantizando idempotencia por (canal, escalón). Muta <c>incident.Deliveries</c>; el llamante persiste.
/// Feature: F05.01.
/// </summary>
public sealed class NotificationDispatcher
{
    private readonly IReadOnlyDictionary<ChannelType, INotificationChannel> _channels;
    private readonly IUserRepository _users;
    private readonly WalletService _wallet;
    private readonly INotificationLogRepository _logs;
    private readonly IAckLinkBuilder _ackLinks;
    private readonly IClock _clock;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        IUserRepository users,
        WalletService wallet,
        INotificationLogRepository logs,
        IAckLinkBuilder ackLinks,
        IClock clock,
        ILogger<NotificationDispatcher> logger)
    {
        _channels = channels.ToDictionary(c => c.Channel);
        _users = users;
        _wallet = wallet;
        _logs = logs;
        _ackLinks = ackLinks;
        _clock = clock;
        _logger = logger;
    }

    public async Task DispatchDueAsync(Watch watch, Incident incident, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(watch.UserId, ct);
        if (user is null) return;

        var now = _clock.UtcNow;
        var tz = SafeTimeZone(user.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);

        foreach (var binding in watch.ChannelBindings.Where(b => b.Enabled).OrderBy(b => b.Order))
        {
            var step = binding.Order;

            // ¿Ha llegado su turno de escalado?
            if (incident.OpenedAtUtc.Add(binding.EscalationDelay) > now) continue;
            // ¿Ya entregado/fallado definitivamente?
            if (incident.HasDispatched(binding.ChannelType, step)) continue;

            // Quiet hours: se pospone (se reintenta en un tick posterior).
            if (binding.QuietHours?.IsQuiet(localNow) == true)
            {
                MarkSkippedOnce(incident, binding.ChannelType, step, "quiet hours");
                continue;
            }

            if (!_channels.TryGetValue(binding.ChannelType, out var channel))
            {
                RecordFinal(incident, binding.ChannelType, step, null, AlertDeliveryStatus.Failed, now, 0m, null, "canal no disponible");
                continue;
            }

            var destination = ResolveDestination(binding, user);
            if (string.IsNullOrWhiteSpace(destination))
            {
                RecordFinal(incident, binding.ChannelType, step, null, AlertDeliveryStatus.Failed, now, 0m, null, "sin destino configurado");
                continue;
            }

            // Canales metered: requieren teléfono verificado y saldo suficiente.
            decimal charged = 0m;
            if (channel.IsMetered)
            {
                if (!user.PhoneVerified)
                {
                    MarkSkippedOnce(incident, binding.ChannelType, step, "teléfono no verificado");
                    continue;
                }

                var charge = await _wallet.ChargeForMessageAsync(user.Id, binding.ChannelType, destination, ct);
                if (charge.IsInsufficient)
                {
                    MarkSkippedOnce(incident, binding.ChannelType, step, "créditos insuficientes");
                    await NotifyLowBalanceOnceAsync(user, watch, incident, ct);
                    continue; // se reintentará cuando recargue
                }
                charged = charge.Cost;
            }

            var message = BuildMessage(watch, incident, binding, destination!, user);
            NotificationResult result;
            try
            {
                result = await channel.SendAsync(message, ct);
            }
            catch (Exception ex)
            {
                result = NotificationResult.Fail(ex.Message);
            }

            if (!result.Success && charged > 0m)
            {
                await _wallet.RefundAsync(user.Id, charged, "envío metered fallido", ct);
                charged = 0m;
            }

            RecordFinal(incident, binding.ChannelType, step, destination,
                result.Success ? AlertDeliveryStatus.Sent : AlertDeliveryStatus.Failed,
                now, charged, result.ProviderMessageId, result.Error);

            incident.LastEscalatedAtUtc = now;
            if (step > incident.CurrentEscalationStep) incident.CurrentEscalationStep = step;

            await _logs.InsertAsync(new NotificationLog
            {
                UserId = user.Id,
                WatchId = watch.Id,
                IncidentId = incident.Id,
                Channel = binding.ChannelType,
                Destination = destination!,
                Kind = "alert",
                Success = result.Success,
                CreditsCharged = charged,
                ProviderMessageId = result.ProviderMessageId,
                Error = result.Error,
                SentAtUtc = now,
            }, ct);

            if (result.Success)
                _logger.LogInformation("Alerta enviada watch={Watch} canal={Channel}", watch.Id, binding.ChannelType);
            else
                _logger.LogWarning("Fallo de alerta watch={Watch} canal={Channel}: {Error}", watch.Id, binding.ChannelType, result.Error);
        }
    }

    private static void RecordFinal(Incident incident, ChannelType channel, int step, string? destination,
        AlertDeliveryStatus status, DateTime now, decimal charged, string? providerMessageId, string? error)
    {
        incident.Deliveries.Add(new AlertDelivery
        {
            Channel = channel,
            Destination = destination,
            EscalationStep = step,
            Status = status,
            SentAtUtc = status == AlertDeliveryStatus.Sent ? now : null,
            CreditsCharged = charged,
            ProviderMessageId = providerMessageId,
            Error = error,
        });
    }

    private static void MarkSkippedOnce(Incident incident, ChannelType channel, int step, string reason)
    {
        var exists = incident.Deliveries.Any(d => d.Channel == channel && d.EscalationStep == step
                                               && d.Status == AlertDeliveryStatus.Skipped);
        if (exists) return;
        incident.Deliveries.Add(new AlertDelivery
        {
            Channel = channel,
            EscalationStep = step,
            Status = AlertDeliveryStatus.Skipped,
            Error = reason,
        });
    }

    private async Task NotifyLowBalanceOnceAsync(User user, Watch watch, Incident incident, CancellationToken ct)
    {
        // Avisa por un canal gratis (Telegram/Email/Push) de que hay que recargar, una vez por incidente.
        var freeBinding = watch.ChannelBindings.FirstOrDefault(b => b.Enabled && !b.ChannelType.IsMetered());
        if (freeBinding is null || !_channels.TryGetValue(freeBinding.ChannelType, out var channel)) return;
        var destination = ResolveDestination(freeBinding, user);
        if (string.IsNullOrWhiteSpace(destination)) return;

        var alreadyWarned = incident.Deliveries.Any(d => d.Error == "low-balance-notice");
        if (alreadyWarned) return;

        var msg = new NotificationMessage
        {
            Channel = freeBinding.ChannelType,
            Destination = destination!,
            Title = "Wardkitten · saldo de créditos insuficiente",
            Body = $"No se pudo alertar por SMS/WhatsApp de '{watch.Name}' por falta de créditos. Recarga tu saldo para reactivar esos canales.",
            WatchId = watch.Id,
            IncidentId = incident.Id,
        };
        try { await channel.SendAsync(msg, ct); } catch { /* best-effort */ }
        incident.Deliveries.Add(new AlertDelivery
        {
            Channel = freeBinding.ChannelType, EscalationStep = -1,
            Status = AlertDeliveryStatus.Sent, SentAtUtc = _clock.UtcNow, Error = "low-balance-notice",
        });
    }

    private NotificationMessage BuildMessage(Watch watch, Incident incident, ChannelBinding binding, string destination, User user)
    {
        var ackUrl = _ackLinks.BuildActionUrl(incident.Id, watch.Id, "done");
        var snoozeUrl = _ackLinks.BuildActionUrl(incident.Id, watch.Id, "snooze");
        return new NotificationMessage
        {
            Channel = binding.ChannelType,
            Destination = destination,
            Severity = watch.Severity,
            Title = $"🐾 Wardkitten · '{watch.Name}' incumplió su programación",
            Body = $"La tarea '{watch.Name}' no se confirmó a tiempo. Si ya la has hecho, márcala como hecha.",
            AckUrl = ackUrl,
            WatchId = watch.Id,
            IncidentId = incident.Id,
            Actions = new[]
            {
                new NotificationAction("✅ Hecho", ackUrl, "done"),
                new NotificationAction("😴 Posponer", snoozeUrl, "snooze"),
            },
        };
    }

    private static string? ResolveDestination(ChannelBinding binding, User user)
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

    private static TimeZoneInfo SafeTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { return TimeZoneInfo.Utc; }
    }
}
