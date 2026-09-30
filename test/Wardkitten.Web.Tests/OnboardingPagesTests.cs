// Feature: F01.04 — asistente de bienvenida (layout, aviso en el panel)
using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Wardkitten.Shared.Contracts;
using Wardkitten.Shared.UI.Auth;
using Wardkitten.Shared.UI.Services;
using Wardkitten.Web.Layout;
using Wardkitten.Web.Onboarding;
using Wardkitten.Web.Pages;

namespace Wardkitten.Web.Tests;

public class OnboardingPagesTests : WebTestBase
{
    private static UserDto Me(bool onboardingCompleted) => new(
        "u1", "ana@example.com", "Ana", "Europe/Madrid", "es", "Free", false, false, null,
        new List<string> { "user" }, false, onboardingCompleted);

    private IRenderedComponent<OnboardingLayout> RenderLayout(RenderFragment? body = null)
        => Render<OnboardingLayout>(p => p.Add(x => x.Body, body ?? (b => b.AddMarkupContent(0, "<p id='body'>cuerpo</p>"))));

    // ---- Layout ----

    [Fact]
    public void Layout_ShowsBrandStepperBodyAndSkipAll_WithoutTheNavbar()
    {
        var cut = RenderLayout();

        cut.Find("#body").TextContent.ShouldBe("cuerpo");
        cut.FindAll("nav.navbar").ShouldBeEmpty();
        cut.Find(".wk-onboarding-header").TextContent.ShouldContain("Wardkitten");
        cut.FindAll(".wk-step").Select(s => string.Concat(s.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))).ShouldBe(new[] { "1Tuperfil", "2Cómoavisarte", "3Tuprimermonitor", "4Listo" });
        cut.Find(".wk-step-current").GetAttribute("aria-current").ShouldBe("step");
        cut.Find(".wk-skip-all").TextContent.ShouldBe("Saltar todo e ir al panel");
    }

    [Fact]
    public void Layout_SkipAll_ClosesTheWizard_AndGoesToTheDashboard()
    {
        Api.On("POST /api/onboarding/skip", null, HttpStatusCode.NoContent);
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/welcome");
        var cut = RenderLayout();

        cut.Find(".wk-skip-all").Click();

        cut.WaitForAssertion(() => nav.Uri.ShouldBe("http://localhost/"));
        Api.Count("POST /api/onboarding/skip").ShouldBe(1);
    }

    [Fact]
    public void Layout_SkipAll_Failure_StaysAndExplains()
    {
        Api.Fail("POST /api/onboarding/skip", "Sin conexión", HttpStatusCode.InternalServerError);
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/welcome");
        var cut = RenderLayout();

        cut.Find(".wk-skip-all").Click();

        cut.WaitForAssertion(() => cut.Find("footer .text-danger").TextContent.ShouldBe("Sin conexión"));
        nav.Uri.ShouldBe("http://localhost/welcome");
    }

    [Fact]
    public void Layout_StepperFollowsTheWizard_AndLetsGoBack()
    {
        Api.On("GET /api/onboarding/state", new OnboardingStateDto(false, "ana@example.com", "Ana", "Europe/Madrid", "es",
            false, null, false, false, true, new List<Domain.Watches.ChannelBinding>(), 0, "Free", 5, 3600));
        var cut = RenderLayout(b =>
        {
            b.OpenComponent<Welcome>(0);
            b.CloseComponent();
        });
        var welcome = cut.FindComponent<Welcome>();

        cut.InvokeAsync(() => welcome.Instance.SkipStep());
        cut.WaitForAssertion(() => cut.Find(".wk-step-current").TextContent.ShouldContain("Cómo avisarte"));
        cut.Find(".wk-step-done").TextContent.ShouldContain("✓");

        cut.Find(".wk-step-done button").Click();

        cut.WaitForAssertion(() => welcome.Instance.Step.ShouldBe(Welcome.WizardStep.Profile));
        cut.Find(".wk-step-current").TextContent.ShouldContain("Tu perfil");
        cut.WaitForAssertion(() => cut.FindAll("#wk-name").Count.ShouldBe(1));   // la página se repinta en el paso pedido
    }

    // ---- Aviso en el panel ----

    private IRenderedComponent<Home> RenderHome(bool onboardingCompleted, int watches = 0)
    {
        // Instancia propia: el contenedor no la desecha (solo implementa IAsyncDisposable).
        Services.AddSingleton(new LiveHubConnection(
            new WardkittenApiClient(new HttpClient(Api) { BaseAddress = new Uri(BaseUrl) }), Substitute.For<ITokenStore>()));
        Api.On("GET /api/auth/me", Me(onboardingCompleted));
        Api.On("GET /api/watches", Enumerable.Range(0, watches).Select(i => new WatchDto(
            $"w{i}", $"W{i}", null, Domain.Watches.WatchType.Manual, new Domain.Watches.Schedule(), new Domain.Watches.Tolerance(),
            new List<Domain.Watches.ChannelBinding>(), Domain.Watches.Severity.Medium, Domain.Watches.WatchStatus.New, false,
            null, null, 0, null, new List<string>(), null, null, 0, 0, null, 0, DateTime.UtcNow, null)).ToList());
        return Render<Home>();
    }

    [Fact]
    public void Home_PendingOnboarding_ShowsTheBanner_AndBothEmptyStateActions()
    {
        var cut = RenderHome(onboardingCompleted: false);

        cut.WaitForAssertion(() => cut.Find(".wk-onboarding-banner").TextContent.ShouldContain("Termina de configurar tu cuenta"));
        cut.Find(".wk-onboarding-banner a").GetAttribute("href").ShouldBe("welcome");
        var actions = cut.FindAll(".text-center a.btn").Select(a => (a.TextContent, a.GetAttribute("href") ?? "")).ToList();
        actions.ShouldBe(new[] { ("Configurar paso a paso", "welcome"), ("Crear monitor", "watches/new") });
    }

    [Fact]
    public void Home_DismissingTheBanner_SkipsTheWizard()
    {
        Api.On("POST /api/onboarding/skip", null, HttpStatusCode.NoContent);
        var cut = RenderHome(onboardingCompleted: false, watches: 1);
        cut.WaitForAssertion(() => cut.FindAll(".wk-onboarding-banner").Count.ShouldBe(1));

        cut.Find(".wk-onboarding-banner button").Click();

        cut.WaitForAssertion(() => cut.FindAll(".wk-onboarding-banner").ShouldBeEmpty());
        Api.Count("POST /api/onboarding/skip").ShouldBe(1);
    }

    [Fact]
    public void Home_DismissFailure_KeepsTheBanner()
    {
        Api.Fail("POST /api/onboarding/skip", "boom", HttpStatusCode.InternalServerError);
        var cut = RenderHome(onboardingCompleted: false, watches: 1);
        cut.WaitForAssertion(() => cut.FindAll(".wk-onboarding-banner").Count.ShouldBe(1));

        cut.Find(".wk-onboarding-banner button").Click();

        cut.WaitForAssertion(() => Api.Count("POST /api/onboarding/skip").ShouldBe(1));
        cut.FindAll(".wk-onboarding-banner").Count.ShouldBe(1);
    }

    [Fact]
    public void Home_CompletedOnboarding_HasNoBanner()
    {
        var cut = RenderHome(onboardingCompleted: true);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Configurar paso a paso"));
        cut.FindAll(".wk-onboarding-banner").ShouldBeEmpty();
    }
}
