// Feature: F03.03 — banco de pruebas de la URL de ping (dry-run)
using Shouldly;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Domain;

public class PingTestModeTests
{
    private static readonly DateTime Now = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);

    private static Watch DueWatch() => new()
    {
        Id = "w1",
        UserId = "u1",
        Type = WatchType.Ping,
        Schedule = new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 3600 },
        Tolerance = new Tolerance { GraceSeconds = 0, SkipTolerance = 0 },
        Status = WatchStatus.Up,
        NextDueAtUtc = Now.AddHours(-2),
    };

    [Fact]
    public void TestMode_SuspendsEvaluation()
    {
        var watch = DueWatch();
        watch.IsActiveForEvaluation(Now).ShouldBeTrue();

        watch.StartTestMode(Now.AddMinutes(15));

        watch.IsTestMode(Now).ShouldBeTrue();
        // Mientras se ensaya la URL no se juzga la vigilancia: el ensayo no puede provocar una alerta.
        watch.IsActiveForEvaluation(Now).ShouldBeFalse();
    }

    [Fact]
    public void TestMode_ExpiresByItself()
    {
        var watch = DueWatch();
        watch.StartTestMode(Now.AddMinutes(15));

        watch.IsTestMode(Now.AddMinutes(16)).ShouldBeFalse();
        watch.IsActiveForEvaluation(Now.AddMinutes(16)).ShouldBeTrue();
    }

    [Fact]
    public void EndTestMode_ReschedulesWhenDeadlinePassedDuringTheTest()
    {
        var watch = DueWatch();
        watch.NextDueAtUtc = Now.AddMinutes(5);        // aún no vencía al empezar la prueba
        watch.StartTestMode(Now.AddMinutes(15));

        var after = Now.AddMinutes(20);                 // el deadline pasó durante el ensayo
        watch.EndTestMode(after);

        watch.TestModeUntilUtc.ShouldBeNull();
        watch.ConsecutiveMisses.ShouldBe(0);
        watch.NextDueAtUtc.ShouldBe(after.AddHours(1)); // reprogramada, sin incumplimiento
    }

    [Fact]
    public void EndTestMode_KeepsScheduleWhenDeadlineStillAhead()
    {
        var watch = DueWatch();
        watch.NextDueAtUtc = Now.AddHours(1);
        watch.StartTestMode(Now.AddMinutes(15));

        watch.EndTestMode(Now.AddMinutes(5));

        watch.NextDueAtUtc.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void CloseExpiredTestMode_OnlyActsOnceTheWindowIsOver()
    {
        var watch = DueWatch();
        watch.StartTestMode(Now.AddMinutes(15));

        watch.CloseExpiredTestMode(Now).ShouldBeFalse();
        watch.TestModeUntilUtc.ShouldNotBeNull();

        watch.CloseExpiredTestMode(Now.AddMinutes(16)).ShouldBeTrue();
        watch.TestModeUntilUtc.ShouldBeNull();
        watch.CloseExpiredTestMode(Now.AddMinutes(17)).ShouldBeFalse();  // idempotente
    }

    [Fact]
    public void Probe_KeepsOnlyTheMostRecentHits()
    {
        var probe = new PingProbe { Token = "t", ExpiresAtUtc = Now.AddHours(1) };

        for (var i = 0; i < PingProbe.MaxHits + 10; i++)
            probe.RegisterHit(new PingProbeHit { ReceivedAtUtc = Now.AddSeconds(i) });

        probe.HitCount.ShouldBe(PingProbe.MaxHits + 10);        // el contador no se recorta
        probe.Hits.Count.ShouldBe(PingProbe.MaxHits);           // el historial sí
        probe.Hits[^1].ReceivedAtUtc.ShouldBe(Now.AddSeconds(PingProbe.MaxHits + 9));
        probe.LastHitAtUtc.ShouldBe(Now.AddSeconds(PingProbe.MaxHits + 9));
    }

    [Fact]
    public void Probe_IsNotActiveOnceExpired()
    {
        var probe = new PingProbe { Token = "t", ExpiresAtUtc = Now };

        probe.IsActive(Now.AddSeconds(-1)).ShouldBeTrue();
        probe.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void DryRunWindow_IsClamped()
    {
        PingProbe.ClampDryRunWindow(TimeSpan.Zero).ShouldBe(PingProbe.DefaultDryRunWindow);
        PingProbe.ClampDryRunWindow(TimeSpan.FromDays(1)).ShouldBe(PingProbe.MaxDryRunWindow);
        PingProbe.ClampDryRunWindow(TimeSpan.FromMinutes(5)).ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Payload_IsTrimmed()
    {
        PingProbe.TrimPayload(null).ShouldBeNull();
        PingProbe.TrimPayload("   ").ShouldBeNull();
        PingProbe.TrimPayload(new string('x', PingProbe.MaxPayloadChars + 100))!.Length.ShouldBe(PingProbe.MaxPayloadChars);
    }
}
