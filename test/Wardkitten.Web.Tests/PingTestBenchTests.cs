// Feature: F03.03 — banco de pruebas de la URL de ping (componente reutilizable)
using System.Net;
using Bunit;
using Shouldly;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Shared.Contracts;
using Wardkitten.Shared.UI.Components;

namespace Wardkitten.Web.Tests;

public class PingTestBenchTests : WebTestBase
{
    private const string Url = "https://www.wardkitten.test/p/tok123";

    private static PingTestStateDto Draft(params PingTestHitDto[] hits) => new(
        "probe-1", "tok123", Url, PingProbeMode.Draft, DateTime.UtcNow.AddHours(2), null,
        hits.Length > 0 ? hits[0].ReceivedAtUtc : null, hits.Length, hits.ToList());

    private static PingTestHitDto Hit(DateTime at, bool counted = false, string kind = "Success")
        => new(at, kind, counted ? "Ping" : "Prueba", counted, "GET", "10.0.0.1", "curl", null);

    [Fact]
    public void AutoStart_ReservesAUrl_AndShowsUrlCopyCurlAndWaiting()
    {
        Api.On("POST /api/ping-tests", Draft());

        var cut = Render<PingTestBench>(p => p.Add(x => x.WaitingText, "Esperando la primera llamada…"));

        cut.WaitForAssertion(() => cut.Find("input[aria-label='URL de ping']").GetAttribute("value").ShouldBe(Url));
        cut.Markup.ShouldContain($"curl -fsS {Url}");
        cut.Markup.ShouldContain("Esperando la primera llamada…");
        cut.Markup.ShouldContain("no cuentan");
        cut.Find("button.btn-outline-secondary").TextContent.ShouldBe("Terminar prueba");
        cut.Instance.ProbeId.ShouldBe("probe-1");
        Api.LastBody<StartPingTestRequest>("POST /api/ping-tests")!.WatchId.ShouldBeNull();
    }

    [Fact]
    public void Inactive_RendersNothing_AndDoesNotStart()
    {
        var cut = Render<PingTestBench>(p => p.Add(x => x.Active, false));
        cut.Markup.Trim().ShouldBeEmpty();
        Api.Count("POST /api/ping-tests").ShouldBe(0);
    }

    [Fact]
    public void WithoutAutoStart_OffersTheButton()
    {
        Api.On("POST /api/ping-tests", Draft());
        var cut = Render<PingTestBench>(p => p.Add(x => x.AutoStart, false));

        Api.Count("POST /api/ping-tests").ShouldBe(0);
        cut.Markup.ShouldContain("Todavía no ha llegado ninguna solicitud.");
        cut.Find("button").TextContent.Trim().ShouldBe("Obtener URL de prueba");

        cut.Find("button").Click();
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBe("probe-1"));
    }

    [Fact]
    public void StartFailure_ShowsTheError()
    {
        Api.Fail("POST /api/ping-tests", "Demasiadas pruebas");
        var cut = Render<PingTestBench>();
        cut.WaitForAssertion(() => cut.Find(".alert-danger").TextContent.ShouldBe("Demasiadas pruebas"));
        cut.Instance.ProbeId.ShouldBeNull();
    }

    [Fact]
    public void Polling_ShowsIncomingTestHits()
    {
        var hit = Hit(DateTime.UtcNow.AddSeconds(-5));
        Api.On("POST /api/ping-tests", Draft());
        Api.On("GET /api/ping-tests/probe-1", Draft(hit));

        var cut = Render<PingTestBench>(p => p
            .Add(x => x.TestPollInterval, TimeSpan.FromMilliseconds(20))
            .Add(x => x.HistoryOpen, true));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Historial de solicitudes (1)"));
        cut.Find("details").HasAttribute("open").ShouldBeTrue();
        cut.Find(".badge.text-bg-secondary").TextContent.ShouldBe("prueba");
        cut.Markup.ShouldContain("Última solicitud:");
        cut.Markup.ShouldContain("(GET)");
        cut.Markup.ShouldContain("10.0.0.1");
    }

    [Fact]
    public void ExpiredProbe_IsDroppedOnPoll()
    {
        Api.On("POST /api/ping-tests", Draft());
        Api.Fail("GET /api/ping-tests/probe-1", "La prueba ha caducado.", HttpStatusCode.NotFound);

        var cut = Render<PingTestBench>(p => p
            .Add(x => x.AutoStart, false)
            .Add(x => x.TestPollInterval, TimeSpan.FromMilliseconds(20)));
        cut.Find("button").Click();

        cut.WaitForAssertion(() => Api.Count("GET /api/ping-tests/probe-1").ShouldBeGreaterThan(0));
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBeNull());
    }

    [Fact]
    public void SavedUrl_ShowsRealCheckIns_AndDoesNotReserveANewUrl()
    {
        Api.On("GET /api/watches/w1/checkins", new[] { new CheckInDto("c1", "Success", "Ping", DateTime.UtcNow.AddMinutes(-1), null) });

        var cut = Render<PingTestBench>(p => p
            .Add(x => x.WatchId, "w1")
            .Add(x => x.SavedPingToken, "saved")
            .Add(x => x.TestPollInterval, TimeSpan.FromMilliseconds(10))
            .Add(x => x.HistoryPollInterval, TimeSpan.FromMilliseconds(20)));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Historial de solicitudes (1)"));
        cut.Find("input[aria-label='URL de ping']").GetAttribute("value").ShouldBe($"{BaseUrl}p/saved");
        cut.Markup.ShouldContain("Esta URL está en producción");
        cut.Find(".badge.text-bg-success:not(:first-child), td .badge").TextContent.ShouldBe("cuenta");
        cut.Markup.ShouldContain("Probar sin que cuente");
        Api.Count("POST /api/ping-tests").ShouldBe(0);
        cut.WaitForAssertion(() => Api.Count("GET /api/watches/w1/checkins").ShouldBeGreaterThan(1));
    }

    [Fact]
    public void SavedUrl_WithATestInProgress_Warns()
    {
        Api.On("GET /api/watches/w1/checkins", Array.Empty<CheckInDto>());
        var cut = Render<PingTestBench>(p => p
            .Add(x => x.WatchId, "w1")
            .Add(x => x.SavedPingToken, "saved")
            .Add(x => x.SavedTestModeUntilUtc, DateTime.UtcNow.AddMinutes(10)));

        cut.Markup.ShouldContain("Hay una prueba en curso");
        cut.Markup.ShouldContain("Ver la prueba");
    }

    [Fact]
    public void DryRun_ShowsTheWindow_AndResumeWhenItEnded()
    {
        var running = Draft() with { Mode = PingProbeMode.DryRun, TestModeUntilUtc = DateTime.UtcNow.AddMinutes(10) };
        Api.On("GET /api/watches/w1/checkins", Array.Empty<CheckInDto>());
        Api.On("POST /api/ping-tests", running);

        var cut = Render<PingTestBench>(p => p.Add(x => x.WatchId, "w1").Add(x => x.SavedPingToken, "tok123"));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Probar sin que cuente").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Prueba en curso hasta las"));
        Api.LastBody<StartPingTestRequest>("POST /api/ping-tests")!.WatchId.ShouldBe("w1");

        cut.Instance.Apply(running with { TestModeUntilUtc = null });
        cut.Render();
        cut.Markup.ShouldContain("La ventana de prueba ha terminado");
        cut.FindAll("button").Select(b => b.TextContent.Trim()).ShouldContain("Reanudar prueba");
    }

    [Fact]
    public async Task Stop_DeletesTheProbe_AndClearsTheDraftHistory()
    {
        Api.On("POST /api/ping-tests", Draft(Hit(DateTime.UtcNow)));
        Api.On("DELETE /api/ping-tests/probe-1", null, HttpStatusCode.NoContent);

        var cut = Render<PingTestBench>();
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBe("probe-1"));

        await cut.InvokeAsync(() => cut.Instance.StopTestAsync());

        Api.Count("DELETE /api/ping-tests/probe-1").ShouldBe(1);
        cut.Instance.ProbeId.ShouldBeNull();
        cut.Instance.Hits.ShouldBeEmpty();
        cut.Instance.LastHit.ShouldBeNull();
    }

    [Fact]
    public async Task Stop_OnASavedWatch_ReloadsTheRealHistory()
    {
        Api.On("GET /api/watches/w1/checkins", Array.Empty<CheckInDto>());
        Api.On("POST /api/ping-tests", Draft() with { Mode = PingProbeMode.DryRun });
        Api.On("DELETE /api/ping-tests/probe-1", null, HttpStatusCode.NoContent);
        var cut = Render<PingTestBench>(p => p.Add(x => x.WatchId, "w1").Add(x => x.SavedPingToken, "tok123"));
        await cut.InvokeAsync(() => cut.Instance.StartTestAsync());
        var before = Api.Count("GET /api/watches/w1/checkins");

        await cut.InvokeAsync(() => cut.Instance.StopTestAsync());

        Api.Count("GET /api/watches/w1/checkins").ShouldBe(before + 1);
    }

    [Fact]
    public async Task Dispose_ClosesAnOpenProbe()
    {
        Api.On("POST /api/ping-tests", Draft());
        Api.On("DELETE /api/ping-tests/probe-1", null, HttpStatusCode.NoContent);
        var cut = Render<PingTestBench>();
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBe("probe-1"));

        await cut.Instance.DisposeAsync();

        Api.Count("DELETE /api/ping-tests/probe-1").ShouldBe(1);
    }

    [Fact]
    public async Task Release_KeepsTheAdoptedProbe_OnDispose()
    {
        Api.On("POST /api/ping-tests", Draft());
        var cut = Render<PingTestBench>();
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBe("probe-1"));

        cut.Instance.Release();
        await cut.Instance.DisposeAsync();

        cut.Instance.ProbeId.ShouldBeNull();
        Api.Count("DELETE /api/ping-tests/probe-1").ShouldBe(0);
    }

    [Fact]
    public async Task AttachToWatch_SwitchesToRealCheckIns_WithTheSameUrl()
    {
        Api.On("POST /api/ping-tests", Draft());
        Api.On("GET /api/watches/w9/checkins", new[] { new CheckInDto("c1", "Start", "Ping", DateTime.UtcNow, null) });
        var cut = Render<PingTestBench>(p => p.Add(x => x.HistoryOpen, true));
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBe("probe-1"));

        await cut.InvokeAsync(() => cut.Instance.AttachToWatchAsync("w9", null));

        cut.Instance.ProbeId.ShouldBeNull();
        cut.Find("input[aria-label='URL de ping']").GetAttribute("value").ShouldBe($"{BaseUrl}p/tok123");
        cut.Markup.ShouldContain("▶ inicio");
        cut.Find("td .badge").TextContent.ShouldBe("cuenta");
        await cut.Instance.DisposeAsync();
        Api.Count("DELETE /api/ping-tests/probe-1").ShouldBe(0);
    }

    [Fact]
    public async Task AttachToWatch_PrefersTheTokenOfTheCreatedWatch()
    {
        Api.On("GET /api/watches/w9/checkins", Array.Empty<CheckInDto>());
        var cut = Render<PingTestBench>(p => p.Add(x => x.AutoStart, false).Add(x => x.HistoryOpen, true));

        await cut.InvokeAsync(() => cut.Instance.AttachToWatchAsync("w9", "fresh"));

        cut.Instance.PingUrl.ShouldBe($"{BaseUrl}p/fresh");
        cut.Markup.ShouldContain("spinner-border");   // esperando la primera llamada real
    }

    [Fact]
    public async Task PollOnce_DoesNothing_WhenInactiveOrWithoutUrl()
    {
        var cut = Render<PingTestBench>(p => p.Add(x => x.AutoStart, false));
        (await cut.InvokeAsync(() => cut.Instance.PollOnceAsync())).ShouldBeFalse();

        cut.Render(p => p.Add(x => x.Active, false));
        (await cut.InvokeAsync(() => cut.Instance.PollOnceAsync())).ShouldBeFalse();
    }

    [Fact]
    public async Task HistoryIsPolledLessOften_ButATestOnEveryTick()
    {
        Api.On("GET /api/watches/w1/checkins", Array.Empty<CheckInDto>());
        Api.On("POST /api/ping-tests", Draft() with { Mode = PingProbeMode.DryRun });
        var cut = Render<PingTestBench>(p => p
            .Add(x => x.WatchId, "w1")
            .Add(x => x.SavedPingToken, "tok123")
            .Add(x => x.HistoryPollInterval, TimeSpan.FromMinutes(1)));

        var now = DateTime.UtcNow;   // el histórico se acaba de cargar al montar
        cut.Instance.IsPollDue(now).ShouldBeFalse();
        cut.Instance.IsPollDue(now.AddSeconds(59)).ShouldBeFalse();
        cut.Instance.IsPollDue(now.AddMinutes(1).AddSeconds(1)).ShouldBeTrue();

        await cut.InvokeAsync(() => cut.Instance.StartTestAsync());
        cut.Instance.IsPollDue(now).ShouldBeTrue();
    }

    [Fact]
    public void Copy_UsesTheClipboard()
    {
        Api.On("POST /api/ping-tests", Draft());
        var clip = JSInterop.SetupVoid("navigator.clipboard.writeText", Url);
        clip.SetVoidResult();
        var cut = Render<PingTestBench>();
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBe("probe-1"));

        cut.FindAll("button").Single(b => b.TextContent == "Copiar").Click();

        clip.VerifyInvoke("navigator.clipboard.writeText");
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("¡Copiada!"));
    }

    [Fact]
    public void Copy_Failure_ExplainsHowToCopyByHand()
    {
        Api.On("POST /api/ping-tests", Draft());
        JSInterop.SetupVoid("navigator.clipboard.writeText", Url).SetException(new Microsoft.JSInterop.JSException("denied"));
        var cut = Render<PingTestBench>();
        cut.WaitForAssertion(() => cut.Instance.ProbeId.ShouldBe("probe-1"));

        cut.FindAll("button").Single(b => b.TextContent == "Copiar").Click();

        cut.WaitForAssertion(() => cut.Find(".alert-danger").TextContent.ShouldContain("cópiala a mano"));
    }

    [Theory]
    [InlineData(0, "hace 0 s")]
    [InlineData(59, "hace 59 s")]
    [InlineData(60, "hace 1 min")]
    [InlineData(3599, "hace 59 min")]
    [InlineData(3600, "hace 1 h")]
    [InlineData(-10, "hace 0 s")]
    public void Hace_IsHumanReadable(int secondsAgo, string expected)
    {
        var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        PingTestBench.Hace(now.AddSeconds(-secondsAgo), now).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Start", "▶ inicio")]
    [InlineData("Fail", "✖ fallo")]
    [InlineData("Success", "✔ hecho")]
    public void Tipo_LabelsTheKind(string kind, string expected) => PingTestBench.Tipo(kind).ShouldBe(expected);

    [Fact]
    public void Hora_IsLocalTime()
    {
        var utc = new DateTime(2026, 9, 30, 12, 34, 56, DateTimeKind.Utc);
        PingTestBench.Hora(utc).ShouldBe(utc.ToLocalTime().ToString("HH:mm:ss"));
    }
}
