using Shouldly;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Domain;

public class ToleranceAndWatchTests
{
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, true)]
    [InlineData(2, 2, false)]
    [InlineData(2, 3, true)]
    public void Tolerance_IsBreached_RespectsSkipCount(int skip, int misses, bool expected)
    {
        var tolerance = new Tolerance { SkipTolerance = skip };

        tolerance.IsBreached(misses).ShouldBe(expected);
    }

    [Fact]
    public void Watch_RegisterCheckIn_ResetsAndSchedulesNext()
    {
        var now = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);
        var watch = new Watch
        {
            Schedule = new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 3600 },
            Tolerance = new Tolerance { SkipTolerance = 0 },
            ConsecutiveMisses = 3,
            Status = WatchStatus.Down,
            CurrentIncidentId = "inc1",
        };

        watch.RegisterCheckIn(now);

        watch.ConsecutiveMisses.ShouldBe(0);
        watch.Status.ShouldBe(WatchStatus.Up);
        watch.CurrentIncidentId.ShouldBeNull();
        watch.LastCheckInAtUtc.ShouldBe(now);
        watch.NextDueAtUtc.ShouldBe(now.AddHours(1));
    }

    [Fact]
    public void Watch_IsPastDeadline_HonoursGrace()
    {
        var now = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);
        var watch = new Watch
        {
            Tolerance = new Tolerance { GraceSeconds = 600 }, // 10 min de gracia
            NextDueAtUtc = now.AddMinutes(-5),                 // venció hace 5 min
        };

        watch.IsPastDeadline(now).ShouldBeFalse(); // aún dentro de la gracia

        watch.NextDueAtUtc = now.AddMinutes(-15);             // venció hace 15 min
        watch.IsPastDeadline(now).ShouldBeTrue();             // superada la gracia
    }
}
