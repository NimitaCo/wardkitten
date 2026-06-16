using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Notifications;

/// <summary>Entrega/escala las alertas de un incidente por los canales del watch. Ver <see cref="NotificationDispatcher"/>.</summary>
public interface INotificationDispatcher
{
    Task DispatchDueAsync(Watch watch, Incident incident, CancellationToken ct = default);
}
