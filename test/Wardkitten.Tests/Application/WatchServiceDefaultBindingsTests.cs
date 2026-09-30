// Feature: F02.05 — canales por defecto del usuario
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.RealTime;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Application;

/// <summary>
/// Un watch creado sin bindings propios hereda los canales por defecto del usuario (configurados en el
/// asistente de bienvenida); si el usuario no tiene, avisa solo por Email.
/// </summary>
public class WatchServiceDefaultBindingsTests
{
    private static readonly DateTime Now = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);

    private sealed record Harness(WatchService Service, IWatchRepository Watches, IUserRepository Users);

    private static Harness Build(User user)
    {
        var watches = Substitute.For<IWatchRepository>();
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var probes = new PingProbeService(
            Substitute.For<IPingProbeRepository>(), watches, Substitute.For<ICheckInRepository>(),
            Substitute.For<IWatchEventPublisher>(), Options.Create(new NotificationOptions()), new TestClock(Now));
        var service = new WatchService(watches, users, probes, Substitute.For<IWatchEventPublisher>(), new TestClock(Now));
        return new Harness(service, watches, users);
    }

    private static WatchInput Input(List<ChannelBinding> bindings) => new(
        "Backup", null, WatchType.Manual,
        new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 86400, TimeZoneId = "Europe/Madrid" },
        new Tolerance { GraceSeconds = 1800 }, bindings, Severity.Medium, null, null);

    [Fact]
    public async Task Create_WithoutBindings_AndUserWithoutDefaults_UsesEmailOnly()
    {
        var user = new User { Id = "u1", Plan = Plan.Free };
        var h = Build(user);

        var r = await h.Service.CreateAsync("u1", Input(new List<ChannelBinding>()));

        r.Success.ShouldBeTrue();
        var binding = r.Value!.ChannelBindings.ShouldHaveSingleItem();
        binding.ChannelType.ShouldBe(ChannelType.Email);
        binding.Enabled.ShouldBeTrue();
        binding.Order.ShouldBe(0);
    }

    [Fact]
    public async Task Create_WithoutBindings_UsesACopyOfTheUserDefaults()
    {
        var user = new User
        {
            Id = "u1",
            Plan = Plan.Free,
            DefaultChannelBindings = new List<ChannelBinding>
            {
                new() { ChannelType = ChannelType.Telegram, Enabled = true, Order = 0 },
                new() { ChannelType = ChannelType.Slack, Enabled = true, Order = 1, DestinationOverride = "https://hooks.slack.com/x" },
            },
        };
        var h = Build(user);

        var r = await h.Service.CreateAsync("u1", Input(new List<ChannelBinding>()));

        r.Success.ShouldBeTrue();
        var bindings = r.Value!.ChannelBindings;
        bindings.Select(b => b.ChannelType).ShouldBe(new[] { ChannelType.Telegram, ChannelType.Slack });
        bindings[1].DestinationOverride.ShouldBe("https://hooks.slack.com/x");
        bindings[1].Order.ShouldBe(1);
        bindings[0].Enabled.ShouldBeTrue();

        // Copia, no la misma lista: editar el watch no puede tocar los defaults del usuario.
        bindings.ShouldNotBeSameAs(user.DefaultChannelBindings);
        bindings[0].ShouldNotBeSameAs(user.DefaultChannelBindings[0]);
        await h.Watches.Received(1).InsertAsync(Arg.Is<Watch>(w => w.ChannelBindings.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_CopiesEveryBindingSetting()
    {
        var quiet = new QuietHours { StartMinute = 60, EndMinute = 420 };
        var user = new User
        {
            Id = "u1",
            DefaultChannelBindings = new List<ChannelBinding>
            {
                new() { ChannelType = ChannelType.Sms, Enabled = false, Order = 3, EscalationDelaySeconds = 900, QuietHours = quiet },
            },
        };
        var h = Build(user);

        var r = await h.Service.CreateAsync("u1", Input(new List<ChannelBinding>()));

        var b = r.Value!.ChannelBindings.ShouldHaveSingleItem();
        b.ChannelType.ShouldBe(ChannelType.Sms);
        b.Enabled.ShouldBeFalse();
        b.Order.ShouldBe(3);
        b.EscalationDelaySeconds.ShouldBe(900);
        b.QuietHours.ShouldNotBeNull();
        b.QuietHours!.ShouldNotBeSameAs(quiet);
        b.QuietHours.StartMinute.ShouldBe(60);
        b.QuietHours.EndMinute.ShouldBe(420);
    }

    [Fact]
    public async Task Create_WithExplicitBindings_IgnoresTheUserDefaults()
    {
        var user = new User
        {
            Id = "u1",
            DefaultChannelBindings = new List<ChannelBinding> { new() { ChannelType = ChannelType.Telegram } },
        };
        var h = Build(user);

        var r = await h.Service.CreateAsync("u1", Input(new List<ChannelBinding> { new() { ChannelType = ChannelType.Discord, DestinationOverride = "https://discord.com/api/webhooks/1" } }));

        r.Value!.ChannelBindings.ShouldHaveSingleItem().ChannelType.ShouldBe(ChannelType.Discord);
    }
}
