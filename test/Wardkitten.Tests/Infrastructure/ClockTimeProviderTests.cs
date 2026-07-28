using Shouldly;
using Wardkitten.Infrastructure.Time;

namespace Wardkitten.Tests.Infrastructure;

/// <summary>
/// El adaptador IClock -> TimeProvider permite que el MongoLeaseStore del paquete
/// Es.Nimita.Infra.Mongo (que recibe TimeProvider) siga gobernado por el IClock de Wardkitten,
/// incluido el reloj fake de los tests.
/// </summary>
public class ClockTimeProviderTests
{
    [Fact]
    public void GetUtcNow_ReflectsTheInjectedClock()
    {
        var clock = new TestClock(new DateTime(2026, 7, 28, 8, 0, 0, DateTimeKind.Utc));
        var provider = new ClockTimeProvider(clock);

        provider.GetUtcNow().ShouldBe(new DateTimeOffset(clock.UtcNow));
        provider.GetUtcNow().Offset.ShouldBe(TimeSpan.Zero);

        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        provider.GetUtcNow().UtcDateTime.ShouldBe(clock.UtcNow);
    }

    [Fact]
    public void GetUtcNow_TreatsUnspecifiedKindAsUtc()
    {
        // TestClock/configuraciones antiguas pueden entregar DateTime sin Kind: se interpreta como UTC.
        var clock = new TestClock(new DateTime(2026, 7, 28, 8, 0, 0, DateTimeKind.Unspecified));
        var provider = new ClockTimeProvider(clock);

        provider.GetUtcNow().UtcDateTime.ShouldBe(new DateTime(2026, 7, 28, 8, 0, 0, DateTimeKind.Utc));
    }
}
