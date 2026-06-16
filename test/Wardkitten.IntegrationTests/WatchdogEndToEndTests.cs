using EphemeralMongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.DependencyInjection;
using Wardkitten.Application.Evaluation;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;
using Wardkitten.Infrastructure.DependencyInjection;

namespace Wardkitten.IntegrationTests;

/// <summary>
/// Prueba de extremo a extremo del bucle watchdog contra un MongoDB real (EphemeralMongo, sin Docker):
/// crear watch vencido → el motor abre incidente y alerta una vez → check-in → incidente resuelto.
/// Ejercita índices, colección time-series, apertura idempotente de incidentes y dispatcher real.
/// </summary>
public class WatchdogEndToEndTests
{
    private sealed class FakeChannel : INotificationChannel
    {
        public ChannelType Channel => ChannelType.Email;
        public List<NotificationMessage> Sent { get; } = new();
        public Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
        {
            Sent.Add(message);
            return Task.FromResult(NotificationResult.Ok("fake-" + Sent.Count));
        }
    }

    [Fact]
    public async Task FullWatchdogLoop_OnRealMongo()
    {
        using IMongoRunner runner = MongoRunner.Run();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MONGOSETTINGS_CONNECTION"] = runner.ConnectionString,
            ["MONGOSETTINGS_DATABASENAME"] = "WardkittenIT_" + Guid.NewGuid().ToString("N")[..8],
        }).Build();

        var fakeChannel = new FakeChannel();
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddWardkittenInfrastructure(config);
        services.AddWardkittenApplication();
        services.AddSingleton<INotificationChannel>(fakeChannel);

        await using var provider = services.BuildServiceProvider();
        await provider.InitializeWardkittenInfrastructureAsync();

        var users = provider.GetRequiredService<IUserRepository>();
        var wallets = provider.GetRequiredService<IWalletRepository>();
        var watches = provider.GetRequiredService<IWatchRepository>();
        var incidents = provider.GetRequiredService<IIncidentRepository>();
        var engine = provider.GetRequiredService<EvaluationEngine>();
        var checkIns = provider.GetRequiredService<CheckInService>();

        // 1) Usuario + wallet
        var user = new User { Email = "dueno@example.com", DisplayName = "Dueño", Plan = Plan.Pro };
        await users.InsertAsync(user);
        await wallets.GetOrCreateForUserAsync(user.Id);

        // 2) Watch ya vencido (cada 60s, sin gracia ni skips), con canal Email
        var watch = new Watch
        {
            UserId = user.Id,
            Name = "Regar las plantas",
            Type = WatchType.Manual,
            Schedule = new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 60, TimeZoneId = "Europe/Madrid" },
            Tolerance = new Tolerance { GraceSeconds = 0, SkipTolerance = 0 },
            ChannelBindings = new() { new ChannelBinding { ChannelType = ChannelType.Email, Enabled = true } },
            Status = WatchStatus.Up,
            NextDueAtUtc = DateTime.UtcNow.AddMinutes(-2),
        };
        await watches.InsertAsync(watch);

        // 3) El motor evalúa los vencidos
        var processed = await engine.EvaluateDueAsync();

        // --- Asserts: incidente abierto + alerta enviada una vez ---
        processed.ShouldBe(1);
        fakeChannel.Sent.Count.ShouldBe(1);
        fakeChannel.Sent[0].Channel.ShouldBe(ChannelType.Email);
        fakeChannel.Sent[0].Destination.ShouldBe("dueno@example.com");

        var openIncident = await incidents.GetOpenByWatchAsync(watch.Id);
        openIncident.ShouldNotBeNull();
        (await watches.GetByIdAsync(watch.Id))!.Status.ShouldBe(WatchStatus.Down);

        // 4) Un segundo barrido NO debe duplicar alertas (idempotencia)
        await engine.ProcessOpenIncidentsAsync();
        fakeChannel.Sent.Count.ShouldBe(1);

        // 5) Check-in → incidente resuelto y watch al día
        var checkIn = await checkIns.RecordSuccessByWatchIdAsync(watch.Id, CheckInSource.App);
        checkIn.Success.ShouldBeTrue();

        (await incidents.GetOpenByWatchAsync(watch.Id)).ShouldBeNull();
        (await watches.GetByIdAsync(watch.Id))!.Status.ShouldBe(WatchStatus.Up);
    }
}
