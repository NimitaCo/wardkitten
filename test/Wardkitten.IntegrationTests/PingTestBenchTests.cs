// Feature: F03.03 — banco de pruebas de la URL de ping
using EphemeralMongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.DependencyInjection;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;
using Wardkitten.Infrastructure.DependencyInjection;
using Wardkitten.Infrastructure.Mongo;

namespace Wardkitten.IntegrationTests;

/// <summary>
/// Banco de pruebas de la URL de ping contra un MongoDB real: probar durante el alta, adoptar el token al
/// guardar (la URL ensayada es la de producción), ensayar sin que cuente sobre una vigilancia ya guardada
/// y auto-borrado por caducidad. Ejercita servicios, repositorios e índices reales.
/// </summary>
public class PingTestBenchTests
{
    private static ServiceProvider BuildProvider(IMongoRunner runner)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MONGOSETTINGS_CONNECTION"] = runner.ConnectionString,
            ["MONGOSETTINGS_DATABASENAME"] = "WardkittenIT_" + Guid.NewGuid().ToString("N")[..8],
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddWardkittenInfrastructure(config);
        services.AddWardkittenApplication("https://www.wardkitten.test");
        return services.BuildServiceProvider();
    }

    private static WatchInput PingWatchInput(string? probeId) => new(
        "Backup nocturno", null, WatchType.Ping,
        new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 3600, TimeZoneId = "Europe/Madrid" },
        new Tolerance { GraceSeconds = 0, SkipTolerance = 0 },
        new List<ChannelBinding> { new() { ChannelType = ChannelType.Email, Enabled = true } },
        Severity.Medium, null, null, null, 0, probeId);

    [Fact]
    public async Task ProbeDuringCreation_DoesNotCount_AndItsUrlBecomesTheRealOne()
    {
        using IMongoRunner runner = MongoRunner.Run();
        await using var provider = BuildProvider(runner);
        await provider.InitializeWardkittenInfrastructureAsync();

        var users = provider.GetRequiredService<IUserRepository>();
        var wallets = provider.GetRequiredService<IWalletRepository>();
        var watchRepo = provider.GetRequiredService<IWatchRepository>();
        var probeRepo = provider.GetRequiredService<IPingProbeRepository>();
        var probes = provider.GetRequiredService<PingProbeService>();
        var watches = provider.GetRequiredService<WatchService>();
        var checkIns = provider.GetRequiredService<CheckInService>();

        var user = new User { Email = "dueno@example.com", DisplayName = "Dueño", Plan = Plan.Pro };
        await users.InsertAsync(user);
        await wallets.GetOrCreateForUserAsync(user.Id);

        // 1) Durante el alta ya hay URL contra la que disparar, sin vigilancia todavía.
        var start = await probes.StartAsync(user.Id, watchId: null, dryRunWindow: null);
        start.Success.ShouldBeTrue();
        var probeId = start.Value!.ProbeId;
        var token = start.Value.Token;
        start.Value.Mode.ShouldBe(PingProbeMode.Draft);
        start.Value.Url.ShouldBe($"https://www.wardkitten.test/p/{token}");

        // 2) El sistema remoto llama: queda registrado, pero no cuenta.
        var first = await checkIns.RecordByPingTokenAsync(
            token, new PingRequest(CheckInKind.Success, "{\"exit\":0}", "10.0.0.1", "POST", "curl/8.7"));
        first.ShouldBe(PingResolution.Test);

        var state = await probes.GetAsync(probeId, user.Id);
        state.Success.ShouldBeTrue();
        state.Value!.HitCount.ShouldBe(1);
        state.Value.LastHitAtUtc.ShouldNotBeNull();
        state.Value.Activity.Count.ShouldBe(1);
        state.Value.Activity[0].Counted.ShouldBeFalse();
        state.Value.Activity[0].RemoteIp.ShouldBe("10.0.0.1");
        state.Value.Activity[0].Method.ShouldBe("POST");
        state.Value.Activity[0].Payload.ShouldBe("{\"exit\":0}");

        // 3) Al guardar, la vigilancia adopta el token ensayado: misma URL en producción.
        var created = await watches.CreateAsync(user.Id, PingWatchInput(probeId));
        created.Success.ShouldBeTrue();
        created.Value!.PingToken.ShouldBe(token);

        // 4) Ahora esa misma URL sí resetea los contadores.
        (await checkIns.RecordByPingTokenAsync(token, new PingRequest(CheckInKind.Success)))
            .ShouldBe(PingResolution.Recorded);

        var saved = await watchRepo.GetByIdAsync(created.Value.Id);
        saved!.LastCheckInAtUtc.ShouldNotBeNull();
        saved.Status.ShouldBe(WatchStatus.Up);

        // 5) Ensayo (dry-run) sobre la vigilancia ya guardada: la URL deja de contar temporalmente.
        var dry = await probes.StartAsync(user.Id, created.Value.Id, TimeSpan.FromMinutes(10));
        dry.Success.ShouldBeTrue();
        dry.Value!.Mode.ShouldBe(PingProbeMode.DryRun);
        dry.Value.Token.ShouldBe(token);
        dry.Value.TestModeUntilUtc.ShouldNotBeNull();

        var beforeTest = await watchRepo.GetByIdAsync(created.Value.Id);
        beforeTest!.IsTestMode(DateTime.UtcNow).ShouldBeTrue();
        beforeTest.IsActiveForEvaluation(DateTime.UtcNow).ShouldBeFalse();   // el ensayo no dispara alertas

        (await checkIns.RecordByPingTokenAsync(token, new PingRequest(CheckInKind.Success)))
            .ShouldBe(PingResolution.Test);

        var duringTest = await watchRepo.GetByIdAsync(created.Value.Id);
        duringTest!.LastCheckInAtUtc.ShouldBe(beforeTest.LastCheckInAtUtc);   // no contó
        duringTest.CurrentStreak.ShouldBe(beforeTest.CurrentStreak);

        // El historial mezcla las de prueba con los check-ins reales, distinguiéndolas.
        var merged = await probes.GetAsync(dry.Value.ProbeId, user.Id);
        merged.Value!.Activity.Count(a => a.Counted).ShouldBe(1);
        merged.Value.Activity.Count(a => !a.Counted).ShouldBe(2);

        // 6) Terminar la prueba: se borra el banco y la URL vuelve a contar.
        (await probes.StopAsync(dry.Value.ProbeId, user.Id)).Success.ShouldBeTrue();
        (await probeRepo.GetByTokenAsync(token)).ShouldBeNull();
        (await watchRepo.GetByIdAsync(created.Value.Id))!.TestModeUntilUtc.ShouldBeNull();

        (await checkIns.RecordByPingTokenAsync(token, new PingRequest(CheckInKind.Success)))
            .ShouldBe(PingResolution.Recorded);

        // 7) Borrar la vigilancia se lleva por delante sus bancos de pruebas.
        await probes.StartAsync(user.Id, created.Value.Id, TimeSpan.FromMinutes(5));
        (await watches.DeleteAsync(created.Value.Id, user.Id)).Success.ShouldBeTrue();
        (await probeRepo.GetByWatchAsync(created.Value.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task AbandonedProbe_ExpiresAndIsSweptByTheTtlIndex()
    {
        using IMongoRunner runner = MongoRunner.Run();
        await using var provider = BuildProvider(runner);
        await provider.InitializeWardkittenInfrastructureAsync();

        var users = provider.GetRequiredService<IUserRepository>();
        var probeRepo = provider.GetRequiredService<IPingProbeRepository>();
        var probes = provider.GetRequiredService<PingProbeService>();
        var checkIns = provider.GetRequiredService<CheckInService>();
        var ctx = provider.GetRequiredService<MongoContext>();

        var user = new User { Email = "abandona@example.com", Plan = Plan.Free };
        await users.InsertAsync(user);

        var start = await probes.StartAsync(user.Id, watchId: null, dryRunWindow: null);
        var probe = (await probeRepo.GetByTokenAsync(start.Value!.Token))!;

        // Quien empieza un alta y no la termina deja el borrador caducado…
        probe.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await probeRepo.ReplaceAsync(probe);

        // …que deja de aceptar solicitudes de inmediato (sin esperar al barrido TTL, que corre cada ~60 s).
        (await checkIns.RecordByPingTokenAsync(probe.Token, new PingRequest(CheckInKind.Success)))
            .ShouldBe(PingResolution.NotFound);
        (await probes.GetAsync(probe.Id, user.Id)).Success.ShouldBeFalse();
        (await probeRepo.RegisterHitAsync(probe.Id, new PingProbeHit { ReceivedAtUtc = DateTime.UtcNow }, DateTime.UtcNow))
            .ShouldBeFalse();

        // …y que Mongo borra solo: el índice TTL sobre expiresAtUtc existe con caducidad inmediata.
        var indexes = await (await ctx.PingProbes.Indexes.ListAsync()).ToListAsync();
        var ttl = indexes.SingleOrDefault(i => i["name"] == "ttl_pingprobe_expiry");
        ttl.ShouldNotBeNull();
        ttl!["expireAfterSeconds"].ToDouble().ShouldBe(0);
        ttl["key"].AsBsonDocument.Contains("expiresAtUtc").ShouldBeTrue();
    }
}
