// Feature: F01.04 — asistente de bienvenida (piezas auxiliares)
using Shouldly;
using Wardkitten.Shared.UI.Auth;
using Wardkitten.Web.Onboarding;

namespace Wardkitten.Web.Tests;

public class OnboardingHelpersTests
{
    // ---- Periodicidades ----

    [Fact]
    public void Presets_FreePlan_StartAtOneHour()
        => IntervalPresets.For(3600).Select(p => p.Seconds).ShouldBe(new[] { 3600, 21600, 43200, 86400, 604800 });

    [Fact]
    public void Presets_PaidPlans_OfferMinutes()
    {
        IntervalPresets.For(60).Count.ShouldBe(IntervalPresets.All.Count);
        IntervalPresets.For(300).First().Label.ShouldBe("Cada 5 minutos");
        IntervalPresets.For(301).First().Seconds.ShouldBe(900);
    }

    [Fact]
    public void Presets_HaveHumanLabels()
        => IntervalPresets.All.Select(p => p.Label).ShouldBe(new[]
        {
            "Cada 5 minutos", "Cada 15 minutos", "Cada 30 minutos", "Cada hora", "Cada 6 horas", "Cada 12 horas", "Cada día", "Cada semana",
        });

    [Fact]
    public void Pick_KeepsThePreferred_OrTheClosestAllowed()
    {
        var free = IntervalPresets.For(3600);
        IntervalPresets.Pick(free, 86400).ShouldBe(86400);
        IntervalPresets.Pick(free, 300).ShouldBe(3600);
        IntervalPresets.Pick(free, 50000).ShouldBe(43200);
        IntervalPresets.Pick(Array.Empty<IntervalPreset>(), 300).ShouldBe(86400);
    }

    // ---- Zonas horarias ----

    [Fact]
    public void TimeZones_AreSortedByOffset_WithReadableLabels()
    {
        var zones = new[]
        {
            TimeZoneInfo.CreateCustomTimeZone("Europe/Madrid", TimeSpan.FromHours(1), "Madrid", "Madrid"),
            TimeZoneInfo.CreateCustomTimeZone("America/New_York", TimeSpan.FromHours(-5), "NY", "NY"),
            TimeZoneInfo.CreateCustomTimeZone("Asia/Kolkata", new TimeSpan(5, 30, 0), "Kolkata", "Kolkata"),
            TimeZoneInfo.CreateCustomTimeZone("Africa/Abidjan", TimeSpan.Zero, "Abidjan", "Abidjan"),
            TimeZoneInfo.CreateCustomTimeZone("Africa/Accra", TimeSpan.Zero, "Accra", "Accra"),
        };

        var options = TimeZoneOptions.Build(zones);

        options.Select(o => o.Id).ShouldBe(new[] { "America/New_York", "Africa/Abidjan", "Africa/Accra", "Europe/Madrid", "Asia/Kolkata" });
        options[0].Label.ShouldBe("(UTC-05:00) America/New York");
        options[1].Label.ShouldBe("(UTC+00:00) Africa/Abidjan");
        options[4].Label.ShouldBe("(UTC+05:30) Asia/Kolkata");
    }

    [Fact]
    public void Pick_PrefersTheSavedZone()
    {
        var options = new List<TimeZoneOption> { new("Europe/Madrid", "M"), new("Atlantic/Canary", "C") };
        TimeZoneOptions.Pick(options, "Atlantic/Canary", "Europe/Madrid").ShouldBe("Atlantic/Canary");
        options.Count.ShouldBe(2);
    }

    [Fact]
    public void Pick_FallsBackToTheBrowser_ThenUtc()
    {
        var options = new List<TimeZoneOption> { new("Europe/Madrid", "M") };
        TimeZoneOptions.Pick(options, "  ", "Europe/Madrid").ShouldBe("Europe/Madrid");
        options.Count.ShouldBe(1);

        TimeZoneOptions.Pick(options, null, null).ShouldBe("UTC");
        options[0].ShouldBe(new TimeZoneOption("UTC", "UTC"));
    }

    [Fact]
    public void Pick_AddsAZoneThatIsNotListed()
    {
        var options = new List<TimeZoneOption> { new("Europe/Madrid", "M") };
        TimeZoneOptions.Pick(options, " Pacific/Auckland ", "Europe/Madrid").ShouldBe("Pacific/Auckland");
        options[0].Id.ShouldBe("Pacific/Auckland");
        options.Count.ShouldBe(2);
    }

    // ---- Progreso ----

    [Fact]
    public async Task Progress_TracksCurrentAndFurthest_AndOnlyGoesBackToVisitedSteps()
    {
        var progress = new OnboardingProgress();
        var changes = 0;
        var requested = new List<int>();
        progress.Changed += () => changes++;

        await progress.RequestStepAsync(0);   // sin página suscrita: no hace nada
        progress.StepRequested += step => { requested.Add(step); return Task.CompletedTask; };

        progress.Set(2);
        progress.Set(1);
        changes.ShouldBe(2);
        progress.Current.ShouldBe(1);
        progress.Furthest.ShouldBe(2);

        progress.CanGoTo(0).ShouldBeTrue();
        progress.CanGoTo(1).ShouldBeFalse();   // ya está ahí
        progress.CanGoTo(2).ShouldBeTrue();
        progress.CanGoTo(3).ShouldBeFalse();   // aún no visitado
        progress.CanGoTo(-1).ShouldBeFalse();

        await progress.RequestStepAsync(3);
        await progress.RequestStepAsync(1);
        await progress.RequestStepAsync(2);
        requested.ShouldBe(new[] { 2 });
        OnboardingProgress.StepTitles.ShouldBe(new[] { "Tu perfil", "Cómo avisarte", "Tu primer monitor", "Listo" });
    }

    // ---- Idioma del navegador ----

    [Theory]
    [InlineData("en", "en")]
    [InlineData(" EN ", "en")]
    [InlineData("es", "es")]
    [InlineData("fr", "es")]
    [InlineData("", "es")]
    [InlineData(null, "es")]
    public void BrowserLocale_OnlySpanishOrEnglish(string? twoLetter, string expected)
        => BrowserLocale.Normalize(twoLetter).ShouldBe(expected);

    [Fact]
    public void BrowserLocale_Current_UsesTheUiCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en-GB");
            BrowserLocale.Current().ShouldBe("en");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
            BrowserLocale.Current().ShouldBe("es");
        }
        finally { System.Globalization.CultureInfo.CurrentUICulture = previous; }
    }
}
