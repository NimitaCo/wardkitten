using Wardkitten.Application.Abstractions;

namespace Wardkitten.Infrastructure.Time;

/// <summary>
/// Adaptador <see cref="IClock"/> → <see cref="TimeProvider"/>. Los tipos promovidos a
/// Es.Nimita.Infra.Mongo (p. ej. su <c>MongoLeaseStore</c>) reciben <see cref="TimeProvider"/>;
/// este adaptador mantiene el reloj de Wardkitten (y el fake de los tests) como única fuente de tiempo.
/// </summary>
public sealed class ClockTimeProvider : TimeProvider
{
    private readonly IClock _clock;

    public ClockTimeProvider(IClock clock) => _clock = clock;

    public override DateTimeOffset GetUtcNow()
        => new(DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc));
}
