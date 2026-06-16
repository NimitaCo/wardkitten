using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Evaluation;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Application;

public class EvaluationEngineTests
{
    private static readonly DateTime Now = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);

    private static EvaluationEngine Build(IWatchRepository watches, IIncidentRepository incidents, INotificationDispatcher dispatcher)
        => new(watches, incidents, dispatcher, new TestClock(Now), Substitute.For<ILogger<EvaluationEngine>>());

    [Fact]
    public async Task BreachedWatch_OpensIncidentAndAlertsOnce()
    {
        var watches = Substitute.For<IWatchRepository>();
        var incidents = Substitute.For<IIncidentRepository>();
        var dispatcher = Substitute.For<INotificationDispatcher>();
        incidents.GetOpenByWatchAsync("w1", Arg.Any<CancellationToken>()).Returns((Incident?)null);
        incidents.OpenOrGetExistingAsync(Arg.Any<Incident>(), Arg.Any<CancellationToken>()).Returns(c => (Incident)c[0]);

        var engine = Build(watches, incidents, dispatcher);
        var watch = new Watch
        {
            Id = "w1",
            UserId = "u1",
            Schedule = new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 3600 },
            Tolerance = new Tolerance { GraceSeconds = 0, SkipTolerance = 0 },
            Status = WatchStatus.Up,
            NextDueAtUtc = Now.AddHours(-2),
            ChannelBindings = new() { new ChannelBinding { ChannelType = ChannelType.Email, Enabled = true } },
        };

        var changed = await engine.EvaluateWatchAsync(watch, Now);

        changed.ShouldBeTrue();
        watch.Status.ShouldBe(WatchStatus.Down);
        watch.ConsecutiveMisses.ShouldBeGreaterThan(0);
        await dispatcher.Received(1).DispatchDueAsync(watch, Arg.Any<Incident>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WatchWithinGrace_DoesNotAlert()
    {
        var watches = Substitute.For<IWatchRepository>();
        var incidents = Substitute.For<IIncidentRepository>();
        var dispatcher = Substitute.For<INotificationDispatcher>();

        var engine = Build(watches, incidents, dispatcher);
        var watch = new Watch
        {
            Id = "w2",
            UserId = "u1",
            Schedule = new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 3600 },
            Tolerance = new Tolerance { GraceSeconds = 3600, SkipTolerance = 0 },
            Status = WatchStatus.Up,
            NextDueAtUtc = Now.AddMinutes(-5),
        };

        await engine.EvaluateWatchAsync(watch, Now);

        await dispatcher.DidNotReceive().DispatchDueAsync(Arg.Any<Watch>(), Arg.Any<Incident>(), Arg.Any<CancellationToken>());
        watch.Status.ShouldBe(WatchStatus.Grace);
    }
}
