using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.RealTime;

/// <summary>Publica cambios en tiempo real (SignalR en la API; no-op en el worker). Feature: F08.02.</summary>
public interface IWatchEventPublisher
{
    Task WatchUpdatedAsync(Watch watch, CancellationToken ct = default);
    Task IncidentOpenedAsync(Incident incident, CancellationToken ct = default);
    Task IncidentResolvedAsync(Incident incident, CancellationToken ct = default);
}

public sealed class NoopWatchEventPublisher : IWatchEventPublisher
{
    public Task WatchUpdatedAsync(Watch watch, CancellationToken ct = default) => Task.CompletedTask;
    public Task IncidentOpenedAsync(Incident incident, CancellationToken ct = default) => Task.CompletedTask;
    public Task IncidentResolvedAsync(Incident incident, CancellationToken ct = default) => Task.CompletedTask;
}
