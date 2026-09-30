// Feature: F01.04 — asistente de bienvenida (onboarding)
using NSubstitute;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Tests.Application;

public class OnboardingServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc);

    private sealed record Harness(OnboardingService Service, IUserRepository Users, IWatchRepository Watches, User User);

    private static Harness Build(User? user = null)
    {
        user ??= new User { Id = "u1", Email = "ana@example.com", DisplayName = "Ana", Plan = Plan.Free };
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var watches = Substitute.For<IWatchRepository>();
        return new Harness(new OnboardingService(users, watches, new TestClock(Now)), users, watches, user);
    }

    // ---- Estado ----

    [Fact]
    public async Task GetState_ReturnsUserWatchCountAndPlanLimits()
    {
        var h = Build();
        h.Watches.CountByUserAsync("u1", Arg.Any<CancellationToken>()).Returns(3);

        var r = await h.Service.GetStateAsync("u1");

        r.Success.ShouldBeTrue();
        r.Value!.User.ShouldBeSameAs(h.User);
        r.Value.WatchCount.ShouldBe(3);
        r.Value.Limits.MinIntervalSeconds.ShouldBe(3600);
        r.Value.Completed.ShouldBeFalse();
    }

    [Fact]
    public async Task GetState_Completed_WhenTheDateIsSet()
    {
        var h = Build(new User { Id = "u1", OnboardingCompletedAtUtc = Now });
        (await h.Service.GetStateAsync("u1")).Value!.Completed.ShouldBeTrue();
    }

    [Fact]
    public async Task EveryOperation_FailsForAnUnknownUser()
    {
        var h = Build();
        (await h.Service.GetStateAsync("nope")).Error.ShouldBe("Usuario no encontrado.");
        (await h.Service.UpdateProfileAsync("nope", "Ana", "Europe/Madrid", "es")).Error.ShouldBe("Usuario no encontrado.");
        (await h.Service.SetDefaultChannelsAsync("nope", new List<ChannelBinding>())).Error.ShouldBe("Usuario no encontrado.");
        (await h.Service.CompleteAsync("nope")).Error.ShouldBe("Usuario no encontrado.");
        (await h.Service.SkipAsync("nope")).Error.ShouldBe("Usuario no encontrado.");
        await h.Users.DidNotReceive().ReplaceAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ---- Perfil ----

    [Fact]
    public async Task UpdateProfile_SavesTrimmedValues()
    {
        var h = Build();

        var r = await h.Service.UpdateProfileAsync("u1", "  Ana García ", " America/New_York ", " EN ");

        r.Success.ShouldBeTrue();
        h.User.DisplayName.ShouldBe("Ana García");
        h.User.TimeZoneId.ShouldBe("America/New_York");
        h.User.Locale.ShouldBe("en");
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "Escribe tu nombre.")]
    [InlineData("   ", "Escribe tu nombre.")]
    public async Task UpdateProfile_RequiresAName(string? name, string error)
    {
        var h = Build();
        (await h.Service.UpdateProfileAsync("u1", name, "Europe/Madrid", "es")).Error.ShouldBe(error);
        await h.Users.DidNotReceive().ReplaceAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_LimitsTheNameLength()
    {
        var h = Build();
        (await h.Service.UpdateProfileAsync("u1", new string('a', 80), "Europe/Madrid", "es")).Success.ShouldBeTrue();
        (await h.Service.UpdateProfileAsync("u1", new string('a', 81), "Europe/Madrid", "es"))
            .Error.ShouldBe("El nombre no puede superar 80 caracteres.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mars/Olympus_Mons")]
    public async Task UpdateProfile_RejectsUnknownTimeZones(string? tz)
    {
        var h = Build();
        (await h.Service.UpdateProfileAsync("u1", "Ana", tz, "es")).Error.ShouldBe("Zona horaria no válida.");
        h.User.TimeZoneId.ShouldBe("Europe/Madrid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr")]
    public async Task UpdateProfile_RejectsUnsupportedLocales(string? locale)
    {
        var h = Build();
        (await h.Service.UpdateProfileAsync("u1", "Ana", "Europe/Madrid", locale)).Error.ShouldBe("Idioma no soportado.");
    }

    // ---- Canales por defecto ----

    [Fact]
    public async Task SetDefaultChannels_SavesValidatedAndRenumberedBindings()
    {
        var h = Build(new User { Id = "u1", TelegramChatId = "42" });
        var input = new List<ChannelBinding>
        {
            new() { ChannelType = ChannelType.Email, Order = 7, Enabled = false, EscalationDelaySeconds = -5 },
            new() { ChannelType = ChannelType.Telegram, Order = 3, EscalationDelaySeconds = 600, QuietHours = new QuietHours { StartMinute = 1, EndMinute = 2 } },
            new() { ChannelType = ChannelType.Slack, DestinationOverride = "  https://hooks.slack.com/a  " },
        };

        var r = await h.Service.SetDefaultChannelsAsync("u1", input);

        r.Success.ShouldBeTrue();
        var saved = h.User.DefaultChannelBindings;
        saved.Select(b => b.ChannelType).ShouldBe(new[] { ChannelType.Email, ChannelType.Telegram, ChannelType.Slack });
        saved.Select(b => b.Order).ShouldBe(new[] { 0, 1, 2 });
        saved.ShouldAllBe(b => b.Enabled);
        saved[0].EscalationDelaySeconds.ShouldBe(0);
        saved[0].DestinationOverride.ShouldBeNull();
        saved[1].EscalationDelaySeconds.ShouldBe(600);
        saved[1].QuietHours!.EndMinute.ShouldBe(2);
        saved[2].DestinationOverride.ShouldBe("https://hooks.slack.com/a");
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefaultChannels_DropsExactDuplicates_ButKeepsDifferentUrls()
    {
        var h = Build();
        var input = new List<ChannelBinding>
        {
            new() { ChannelType = ChannelType.Email },
            new() { ChannelType = ChannelType.Email, DestinationOverride = " " },
            new() { ChannelType = ChannelType.Webhook, DestinationOverride = "https://a.example.com/h" },
            new() { ChannelType = ChannelType.Webhook, DestinationOverride = "https://A.example.com/h" },
            new() { ChannelType = ChannelType.Webhook, DestinationOverride = "https://b.example.com/h" },
            new() { ChannelType = ChannelType.Discord, DestinationOverride = "https://b.example.com/h" },
        };

        (await h.Service.SetDefaultChannelsAsync("u1", input)).Success.ShouldBeTrue();

        h.User.DefaultChannelBindings.Select(b => $"{b.ChannelType}:{b.DestinationOverride}").ShouldBe(new[]
        {
            "Email:", "Webhook:https://a.example.com/h", "Webhook:https://b.example.com/h", "Discord:https://b.example.com/h",
        });
    }

    [Fact]
    public async Task SetDefaultChannels_RejectsAChannelThatCannotDeliver_AndSavesNothing()
    {
        var h = Build();
        h.User.DefaultChannelBindings.Add(new ChannelBinding { ChannelType = ChannelType.Email });
        var input = new List<ChannelBinding>
        {
            new() { ChannelType = ChannelType.Email },
            new() { ChannelType = ChannelType.Sms },
        };

        var r = await h.Service.SetDefaultChannelsAsync("u1", input);

        r.Success.ShouldBeFalse();
        r.Error.ShouldBe("Para avisarte por SMS verifica antes tu teléfono.");
        h.User.DefaultChannelBindings.ShouldHaveSingleItem();
        await h.Users.DidNotReceive().ReplaceAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefaultChannels_EmptyOrNull_ResetsToEmailOnlyBehaviour()
    {
        var h = Build();
        h.User.DefaultChannelBindings.Add(new ChannelBinding { ChannelType = ChannelType.Email });

        (await h.Service.SetDefaultChannelsAsync("u1", null)).Success.ShouldBeTrue();

        h.User.DefaultChannelBindings.ShouldBeEmpty();
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
    }

    // ---- Cierre ----

    [Fact]
    public async Task Complete_StampsTheDate()
    {
        var h = Build();
        (await h.Service.CompleteAsync("u1")).Success.ShouldBeTrue();
        h.User.OnboardingCompletedAtUtc.ShouldBe(Now);
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Skip_StampsTheDate()
    {
        var h = Build();
        (await h.Service.SkipAsync("u1")).Success.ShouldBeTrue();
        h.User.OnboardingCompletedAtUtc.ShouldBe(Now);
        await h.Users.Received(1).ReplaceAsync(h.User, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_IsIdempotent_AndKeepsTheFirstDate()
    {
        var first = Now.AddDays(-3);
        var h = Build(new User { Id = "u1", OnboardingCompletedAtUtc = first });

        (await h.Service.CompleteAsync("u1")).Success.ShouldBeTrue();
        (await h.Service.SkipAsync("u1")).Success.ShouldBeTrue();

        h.User.OnboardingCompletedAtUtc.ShouldBe(first);
        await h.Users.DidNotReceive().ReplaceAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
