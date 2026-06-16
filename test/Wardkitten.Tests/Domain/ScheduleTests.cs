using Shouldly;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Domain;

public class ScheduleTests
{
    [Fact]
    public void Interval_AddsSecondsToFrom()
    {
        var schedule = new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 3600 };
        var from = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        var next = schedule.ComputeNextDueUtc(from);

        next.ShouldBe(from.AddHours(1));
    }

    [Fact]
    public void Cron_DailyAtNine_IsNineLocalTime()
    {
        var schedule = new Schedule { Kind = ScheduleKind.Cron, CronExpression = "0 9 * * *", TimeZoneId = "Europe/Madrid" };
        var from = new DateTime(2026, 6, 16, 6, 0, 0, DateTimeKind.Utc); // antes de las 9 local

        var next = schedule.ComputeNextDueUtc(from);

        next.ShouldNotBeNull();
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var local = TimeZoneInfo.ConvertTimeFromUtc(next!.Value, tz);
        local.Hour.ShouldBe(9);
        local.Minute.ShouldBe(0);
    }

    [Fact]
    public void Cron_OnDstSpringForward_DoesNotThrowAndAdvances()
    {
        // Noche del cambio de hora en España (último domingo de marzo 2026 = 29).
        var schedule = new Schedule { Kind = ScheduleKind.Cron, CronExpression = "30 2 * * *", TimeZoneId = "Europe/Madrid" };
        var from = new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc);

        var next = schedule.ComputeNextDueUtc(from);

        next.ShouldNotBeNull();
        next!.Value.ShouldBeGreaterThan(from);
    }

    [Theory]
    [InlineData(ScheduleKind.Interval, 0, null, false)]
    [InlineData(ScheduleKind.Interval, 60, null, true)]
    [InlineData(ScheduleKind.Cron, 0, "no-cron", false)]
    [InlineData(ScheduleKind.Cron, 0, "0 9 * * *", true)]
    public void IsValid_ReportsExpected(ScheduleKind kind, int interval, string? cron, bool expected)
    {
        var schedule = new Schedule { Kind = kind, IntervalSeconds = interval, CronExpression = cron };

        schedule.IsValid(out _).ShouldBe(expected);
    }
}
