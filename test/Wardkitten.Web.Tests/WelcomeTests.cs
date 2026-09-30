// Feature: F01.04 — asistente de bienvenida (onboarding)
using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Watches;
using Wardkitten.Shared.Contracts;
using Wardkitten.Web.Onboarding;
using Wardkitten.Web.Pages;
using Step = Wardkitten.Web.Pages.Welcome.WizardStep;

namespace Wardkitten.Web.Tests;

public class WelcomeTests : WebTestBase
{
    private static OnboardingStateDto State(
        int minInterval = 3600, bool telegramLinked = false, bool telegramAvailable = true, bool phoneVerified = false,
        bool emailVerified = false, List<ChannelBinding>? defaults = null, string locale = "es", string tz = "Europe/Madrid")
        => new(false, "ana@example.com", "Ana", tz, locale, emailVerified, phoneVerified ? "+34600111222" : null,
               phoneVerified, telegramLinked, telegramAvailable, defaults ?? new List<ChannelBinding>(), 0, "Free", 5, minInterval);

    private IRenderedComponent<Welcome> RenderWizard(OnboardingStateDto? state = null, OnboardingProgress? progress = null,
        TimeSpan? telegramPoll = null)
    {
        Api.On("GET /api/onboarding/state", state ?? State());
        return Render<Welcome>(p =>
        {
            if (progress is not null) p.AddCascadingValue(progress);
            if (telegramPoll is not null) p.Add(x => x.TelegramPollInterval, telegramPoll.Value);
        });
    }

    private static Task Do(IRenderedComponent<Welcome> cut, Func<Welcome, Task> action) => cut.InvokeAsync(() => action(cut.Instance));

    private static PingTestStateDto Draft() => new(
        "probe-1", "tok123", "https://www.wardkitten.test/p/tok123", PingProbeMode.Draft, DateTime.UtcNow.AddHours(2),
        null, null, 0, new List<PingTestHitDto>());

    private static WatchDto CreatedWatch(WatchType type, string? token) => new(
        "w1", "Backup", null, type, new Schedule(), new Tolerance(), new List<ChannelBinding>(), Severity.Medium,
        WatchStatus.New, false, null, null, 0, token, new List<string>(), null, null, 0, 0, null, 0, DateTime.UtcNow, null);

    // ---- Carga ----

    [Fact]
    public void Loads_TheState_AndPrefillsTheProfile()
    {
        var cut = RenderWizard(State(locale: "EN"));

        cut.Instance.Loading.ShouldBeFalse();
        cut.Instance.Step.ShouldBe(Step.Profile);
        cut.Instance.DisplayName.ShouldBe("Ana");
        cut.Instance.Locale.ShouldBe("en");
        cut.Instance.TimeZoneId.ShouldBe("Europe/Madrid");
        cut.Instance.TimeZones.ShouldContain(t => t.Id == "Europe/Madrid");
        cut.Find("#wk-name").GetAttribute("value").ShouldBe("Ana");
        cut.Markup.ShouldContain("Los horarios de tus monitores se interpretan en esta zona horaria.");
        cut.Markup.ShouldContain("Esencial");
    }

    [Fact]
    public void LoadFailure_ShowsTheError()
    {
        Api.Fail("GET /api/onboarding/state", "Sesión caducada", HttpStatusCode.Unauthorized);
        var cut = Render<Welcome>();
        cut.Find(".alert-danger").TextContent.ShouldBe("Sesión caducada");
        cut.Instance.State.ShouldBeNull();
    }

    [Fact]
    public void EmptyStateResponse_IsReportedAsAnError()
    {
        Api.On("GET /api/onboarding/state", _ => FakeApi.Respond(System.Text.Json.JsonDocument.Parse("null").RootElement));
        var cut = Render<Welcome>();
        cut.Find(".alert-danger").TextContent.ShouldBe("No se ha podido cargar tu cuenta.");
    }

    [Fact]
    public void Load_TellsTheLayoutWhereWeAre()
    {
        var progress = new OnboardingProgress();
        var changes = 0;
        progress.Changed += () => changes++;
        RenderWizard(progress: progress);
        changes.ShouldBe(1);
    }

    [Fact]
    public void Load_PaidChannels_OnlyTheSavedOnes()
    {
        var cut = RenderWizard(State(phoneVerified: true, defaults: new List<ChannelBinding> { new() { ChannelType = ChannelType.Sms } }));
        cut.Instance.UseSms.ShouldBeTrue();
        cut.Instance.UseWhatsApp.ShouldBeFalse();
    }

    [Fact]
    public void Load_RestoresSavedDefaultChannels()
    {
        var cut = RenderWizard(State(telegramLinked: true, phoneVerified: true, defaults: new List<ChannelBinding>
        {
            new() { ChannelType = ChannelType.Telegram },
            new() { ChannelType = ChannelType.WhatsApp },
            new() { ChannelType = ChannelType.Discord, DestinationOverride = "https://discord.com/api/webhooks/1" },
        }));

        var w = cut.Instance;
        w.UseEmail.ShouldBeFalse();
        w.UseTelegram.ShouldBeTrue();
        w.UseSms.ShouldBeFalse();
        w.UseWhatsApp.ShouldBeTrue();
        w.Urls[ChannelType.Discord].ShouldBe("https://discord.com/api/webhooks/1");
        w.Urls[ChannelType.Slack].ShouldBe(string.Empty);
        w.Phone.ShouldBe("+34600111222");
        w.ChannelSummary.ShouldBe(new[] { "Telegram", "WhatsApp", "Discord" });
    }

    [Fact]
    public void Load_IgnoresDefaultsThatCanNoLongerDeliver()
    {
        var cut = RenderWizard(State(telegramLinked: false, phoneVerified: false, defaults: new List<ChannelBinding>
        {
            new() { ChannelType = ChannelType.Telegram }, new() { ChannelType = ChannelType.Sms },
        }));
        cut.Instance.UseTelegram.ShouldBeFalse();
        cut.Instance.UseSms.ShouldBeFalse();
        cut.Instance.UseEmail.ShouldBeFalse();
    }

    // ---- Paso 1 ----

    [Fact]
    public async Task ContinueProfile_SavesAndMovesOn()
    {
        var progress = new OnboardingProgress();
        var cut = RenderWizard(progress: progress);
        var busyWhileSaving = false;
        Api.On("PUT /api/onboarding/profile", _ => { busyWhileSaving = cut.Instance.Busy; return FakeApi.Respond(new { }); });
        cut.Instance.DisplayName = "Ana García";
        cut.Instance.Locale = "en";

        await Do(cut, w => w.ContinueProfileAsync());

        var sent = Api.LastBody<UpdateProfileRequest>("PUT /api/onboarding/profile")!;
        sent.DisplayName.ShouldBe("Ana García");
        sent.TimeZoneId.ShouldBe("Europe/Madrid");
        sent.Locale.ShouldBe("en");
        cut.Instance.Step.ShouldBe(Step.Channels);
        busyWhileSaving.ShouldBeTrue();
        cut.Instance.Busy.ShouldBeFalse();
        progress.Current.ShouldBe(1);
    }

    [Fact]
    public async Task ContinueProfile_Failure_StaysAndShowsTheError()
    {
        Api.Fail("PUT /api/onboarding/profile", "Zona horaria no válida.");
        var cut = RenderWizard();

        await Do(cut, w => w.ContinueProfileAsync());
        cut.Render();

        cut.Instance.Step.ShouldBe(Step.Profile);
        cut.Instance.Busy.ShouldBeFalse();
        cut.Find(".alert-danger").TextContent.ShouldBe("Zona horaria no válida.");
    }

    [Fact]
    public void SkipStep_ExplainsTheConsequence_AndAdvances()
    {
        var cut = RenderWizard();
        cut.Markup.ShouldContain("Mantendremos los datos con los que creaste la cuenta.");

        cut.FindAll("button").Single(b => b.TextContent == "Saltar este paso").Click();
        cut.Instance.Step.ShouldBe(Step.Channels);
        cut.Markup.ShouldContain("Los monitores nuevos avisarán solo por email.");

        cut.FindAll("button").Single(b => b.TextContent == "Saltar este paso").Click();
        cut.Instance.Step.ShouldBe(Step.FirstWatch);
        cut.Markup.ShouldContain("Podrás crearlo después desde Nuevo o Plantillas.");
        cut.Markup.ShouldContain("Opcional");

        cut.FindAll("button").Single(b => b.TextContent == "Saltar este paso").Click();
        cut.Instance.Step.ShouldBe(Step.Done);
        cut.Markup.ShouldContain("Email (por defecto)");
        cut.Markup.ShouldContain("Aún no tienes monitores");

        cut.Instance.SkipStep();
        cut.Instance.Step.ShouldBe(Step.Done);
    }

    // ---- Paso 2 ----

    [Fact]
    public void BuildDefaultBindings_OnlyIncludesChannelsThatCanDeliver_InOrder()
    {
        var cut = RenderWizard(State(telegramLinked: true, phoneVerified: true));
        var w = cut.Instance;
        w.UseEmail = true;
        w.UseTelegram = true;
        w.UseSms = true;
        w.UseWhatsApp = true;
        w.Urls[ChannelType.Slack] = "  https://hooks.slack.com/x  ";
        w.Urls[ChannelType.Webhook] = "https://example.com/h";
        w.Urls[ChannelType.Discord] = "   ";

        var list = w.BuildDefaultBindings();

        list.Select(b => b.ChannelType).ShouldBe(new[]
        {
            ChannelType.Email, ChannelType.Telegram, ChannelType.Slack, ChannelType.Webhook, ChannelType.Sms, ChannelType.WhatsApp,
        });
        list.Select(b => b.Order).ShouldBe(new[] { 0, 1, 2, 3, 4, 5 });
        list.ShouldAllBe(b => b.Enabled);
        list[2].DestinationOverride.ShouldBe("https://hooks.slack.com/x");
        list[0].DestinationOverride.ShouldBeNull();
    }

    [Fact]
    public void BuildDefaultBindings_SkipsUnlinkedTelegram_AndUnverifiedPhone()
    {
        var cut = RenderWizard();
        var w = cut.Instance;
        w.UseEmail = false;
        w.UseTelegram = true;
        w.UseSms = true;
        w.UseWhatsApp = true;
        w.BuildDefaultBindings().ShouldBeEmpty();
    }

    [Fact]
    public async Task ContinueChannels_WithNothingSelected_AsksForOne()
    {
        var cut = RenderWizard();
        cut.Instance.UseEmail = false;

        await Do(cut, w => w.ContinueChannelsAsync());

        cut.Instance.Error.ShouldBe("Elige al menos un canal: sin ninguno no podríamos avisarte.");
        Api.Count("PUT /api/onboarding/channels").ShouldBe(0);
    }

    [Fact]
    public async Task ContinueChannels_SavesTheDefaults_AndMovesOn()
    {
        // El servidor devuelve lo que de verdad guardó: eso es lo que se resume al final.
        var saved = new List<ChannelBinding> { new() { ChannelType = ChannelType.Slack, DestinationOverride = "https://hooks.slack.com/x" } };
        Api.On("POST /api/ping-tests", Draft());
        var cut = RenderWizard();
        var busyWhileSaving = false;
        Api.On("PUT /api/onboarding/channels", _ => { busyWhileSaving = cut.Instance.Busy; return FakeApi.Respond(saved); });
        cut.Instance.Urls[ChannelType.Slack] = "https://hooks.slack.com/x";

        await Do(cut, w => w.ContinueChannelsAsync());

        Api.LastBody<UpdateChannelsRequest>("PUT /api/onboarding/channels")!.Bindings.Select(b => b.ChannelType)
            .ShouldBe(new[] { ChannelType.Email, ChannelType.Slack });
        cut.Instance.Step.ShouldBe(Step.FirstWatch);
        busyWhileSaving.ShouldBeTrue();
        cut.Instance.Busy.ShouldBeFalse();
        cut.Instance.ChannelSummary.ShouldBe(new[] { "Slack" });
    }

    [Fact]
    public async Task ContinueChannels_Failure_ShowsTheServerReason()
    {
        Api.Fail("PUT /api/onboarding/channels", "La URL de Slack debe ser una dirección https:// válida.");
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Channels);

        await Do(cut, w => w.ContinueChannelsAsync());

        cut.Instance.Step.ShouldBe(Step.Channels);
        cut.Instance.Error.ShouldBe("La URL de Slack debe ser una dirección https:// válida.");
    }

    [Fact]
    public async Task EmailVerification_Flow()
    {
        Api.On("POST /api/auth/email/send-code", null, HttpStatusCode.NoContent);
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Channels);
        cut.Render();
        cut.Markup.ShouldContain("Sin verificar seguirás recibiendo alertas, pero te recomendamos comprobar que llegan");

        await Do(cut, w => w.SendEmailCodeAsync());
        cut.Instance.EmailCodeSent.ShouldBeTrue();
        cut.Instance.EmailMessage.ShouldBe("Te hemos enviado un código a ana@example.com.");

        await Do(cut, w => w.VerifyEmailAsync());
        cut.Instance.EmailMessage.ShouldBe("Escribe el código que te hemos enviado.");

        Api.Fail("POST /api/auth/email/verify", "Código incorrecto.");
        cut.Instance.EmailCode = "111111";
        await Do(cut, w => w.VerifyEmailAsync());
        cut.Instance.EmailVerified.ShouldBeFalse();
        cut.Instance.EmailMessage.ShouldBe("Código incorrecto.");

        Api.On("POST /api/auth/email/verify", null, HttpStatusCode.NoContent);
        cut.Instance.EmailCode = " 123456 ";
        await Do(cut, w => w.VerifyEmailAsync());
        Api.LastBody<VerifyCodeRequest>("POST /api/auth/email/verify")!.Code.ShouldBe("123456");
        cut.Instance.EmailVerified.ShouldBeTrue();
        cut.Instance.EmailMessage.ShouldBe("✅ Email verificado.");
    }

    [Fact]
    public async Task EmailCode_SendFailure_IsShown()
    {
        Api.Fail("POST /api/auth/email/send-code", "Demasiados intentos", HttpStatusCode.TooManyRequests);
        var cut = RenderWizard();
        await Do(cut, w => w.SendEmailCodeAsync());
        cut.Instance.EmailCodeSent.ShouldBeFalse();
        cut.Instance.EmailMessage.ShouldBe("Demasiados intentos");
    }

    [Fact]
    public async Task Telegram_Link_PollsUntilLinked()
    {
        Api.On("POST /api/auth/telegram/link-code", new TelegramLinkCodeDto("abcdefghjkmn", "https://t.me/WardkittenBot?start=abcdefghjkmn", DateTime.UtcNow.AddMinutes(15)));
        var linked = false;
        Api.On("GET /api/auth/telegram/status", _ => FakeApi.Respond(new TelegramStatusDto(linked)));
        var cut = RenderWizard(telegramPoll: TimeSpan.FromMilliseconds(20));
        cut.Instance.GoTo(Step.Channels);
        cut.Render();

        cut.FindAll("button").Single(b => b.TextContent == "Vincular").Click();

        cut.WaitForAssertion(() => cut.Find("a[href='https://t.me/WardkittenBot?start=abcdefghjkmn']").TextContent.ShouldBe("Abrir Telegram"));
        cut.Markup.ShouldContain("/start abcdefghjkmn");
        cut.Markup.ShouldContain("Abre Telegram y pulsa <strong>Start</strong>");
        await Eventually(() => Api.Count("GET /api/auth/telegram/status") > 0);
        cut.Instance.TelegramLinked.ShouldBeFalse();

        linked = true;
        cut.WaitForAssertion(() => cut.Instance.TelegramLinked.ShouldBeTrue());
        cut.Instance.UseTelegram.ShouldBeTrue();
        cut.Instance.TelegramLink.ShouldBeNull();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("✅ Telegram vinculado."));
        var calls = Api.Count("GET /api/auth/telegram/status");
        await Task.Delay(100);
        Api.Count("GET /api/auth/telegram/status").ShouldBe(calls);   // deja de consultar al vincular
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Telegram_Polling_StopsOnUnlinkOrWhenLeaving(bool unlink)
    {
        Api.On("POST /api/auth/telegram/link-code", new TelegramLinkCodeDto("abc", "https://t.me/B?start=abc", DateTime.UtcNow.AddMinutes(15)));
        Api.On("GET /api/auth/telegram/status", new TelegramStatusDto(false));
        Api.On("POST /api/auth/telegram/unlink", null, HttpStatusCode.NoContent);
        var cut = RenderWizard(telegramPoll: TimeSpan.FromMilliseconds(10));
        await Do(cut, w => w.StartTelegramLinkAsync());
        await Eventually(() => Api.Count("GET /api/auth/telegram/status") > 0);

        if (unlink) await Do(cut, w => w.UnlinkTelegramAsync());
        else cut.Instance.Dispose();
        await Task.Delay(50);
        var calls = Api.Count("GET /api/auth/telegram/status");
        await Task.Delay(100);

        Api.Count("GET /api/auth/telegram/status").ShouldBe(calls);
    }

    [Fact]
    public async Task Telegram_NewCode_ReplacesThePreviousPolling()
    {
        Api.On("POST /api/auth/telegram/link-code", new TelegramLinkCodeDto("abc", "https://t.me/B?start=abc", DateTime.UtcNow.AddMinutes(15)));
        Api.On("GET /api/auth/telegram/status", new TelegramStatusDto(false));
        var cut = RenderWizard(telegramPoll: TimeSpan.FromMilliseconds(40));
        await Do(cut, w => w.StartTelegramLinkAsync());
        await Do(cut, w => w.StartTelegramLinkAsync());
        await Do(cut, w => w.StartTelegramLinkAsync());

        await Task.Delay(300);
        cut.Instance.Dispose();
        var calls = Api.Count("GET /api/auth/telegram/status");

        // Un solo bucle vivo: unas 7 consultas en 300 ms, no unas 21.
        calls.ShouldBeLessThan(12);
    }

    [Fact]
    public async Task Telegram_LinkCodeFailure_IsShown()
    {
        Api.Fail("POST /api/auth/telegram/link-code", "Telegram no está disponible en este momento.");
        var cut = RenderWizard();
        await Do(cut, w => w.StartTelegramLinkAsync());
        cut.Instance.TelegramLink.ShouldBeNull();
        cut.Instance.TelegramMessage.ShouldBe("Telegram no está disponible en este momento.");
    }

    [Fact]
    public async Task Telegram_StatusErrors_DoNotLink()
    {
        Api.Fail("GET /api/auth/telegram/status", "boom", HttpStatusCode.InternalServerError);
        var cut = RenderWizard();
        (await cut.InvokeAsync(() => cut.Instance.CheckTelegramStatusAsync())).ShouldBeFalse();
        cut.Instance.TelegramLinked.ShouldBeFalse();
    }

    [Fact]
    public void Telegram_Unavailable_IsExplained()
    {
        var cut = RenderWizard(State(telegramAvailable: false));
        cut.Instance.GoTo(Step.Channels);
        cut.Render();
        cut.Markup.ShouldContain("Telegram no está disponible todavía.");
        cut.FindAll("button").ShouldNotContain(b => b.TextContent == "Vincular");
    }

    [Fact]
    public async Task Telegram_Unlink()
    {
        Api.On("POST /api/auth/telegram/unlink", null, HttpStatusCode.NoContent);
        var cut = RenderWizard(State(telegramLinked: true, defaults: new List<ChannelBinding> { new() { ChannelType = ChannelType.Telegram } }));
        cut.Instance.UseTelegram.ShouldBeTrue();

        await Do(cut, w => w.UnlinkTelegramAsync());

        cut.Instance.TelegramLinked.ShouldBeFalse();
        cut.Instance.UseTelegram.ShouldBeFalse();
        Api.Count("POST /api/auth/telegram/unlink").ShouldBe(1);
    }

    [Fact]
    public async Task Telegram_UnlinkFailure_KeepsTheLink()
    {
        Api.Fail("POST /api/auth/telegram/unlink", "No se pudo");
        var cut = RenderWizard(State(telegramLinked: true));
        await Do(cut, w => w.UnlinkTelegramAsync());
        cut.Instance.TelegramLinked.ShouldBeTrue();
        cut.Instance.TelegramMessage.ShouldBe("No se pudo");
    }

    [Fact]
    public async Task Phone_Verification_Flow()
    {
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Channels);
        cut.Render();
        cut.Markup.ShouldContain("Cada mensaje se cobra con créditos de tu saldo.");

        await Do(cut, w => w.SendPhoneCodeAsync());
        cut.Instance.PhoneMessage.ShouldBe("Escribe tu número de teléfono.");
        Api.Count("POST /api/auth/phone/send-otp").ShouldBe(0);

        Api.On("POST /api/auth/phone/send-otp", null, HttpStatusCode.NoContent);
        cut.Instance.Phone = " 600111222 ";
        await Do(cut, w => w.SendPhoneCodeAsync());
        Api.LastBody<PhoneOtpRequest>("POST /api/auth/phone/send-otp")!.Phone.ShouldBe("600111222");
        cut.Instance.PhoneCodeSent.ShouldBeTrue();
        cut.Instance.PhoneMessage.ShouldBe("Te hemos enviado un SMS con un código.");

        await Do(cut, w => w.VerifyPhoneAsync());
        cut.Instance.PhoneMessage.ShouldBe("Escribe el código del SMS.");

        Api.Fail("POST /api/auth/phone/verify", "Código incorrecto.");
        cut.Instance.PhoneCode = "000000";
        await Do(cut, w => w.VerifyPhoneAsync());
        cut.Instance.PhoneVerified.ShouldBeFalse();
        cut.Instance.PhoneMessage.ShouldBe("Código incorrecto.");

        Api.On("POST /api/auth/phone/verify", null, HttpStatusCode.NoContent);
        cut.Instance.PhoneCode = " 123456 ";
        await Do(cut, w => w.VerifyPhoneAsync());
        Api.LastBody<VerifyCodeRequest>("POST /api/auth/phone/verify")!.Code.ShouldBe("123456");
        cut.Instance.PhoneVerified.ShouldBeTrue();
        cut.Instance.PhoneCodeSent.ShouldBeFalse();
        cut.Instance.PhoneMessage.ShouldBe("✅ Teléfono verificado.");
    }

    [Fact]
    public async Task Phone_NewNumber_ResetsVerificationAndPaidChannels()
    {
        Api.On("POST /api/auth/phone/send-otp", null, HttpStatusCode.NoContent);
        var cut = RenderWizard(State(phoneVerified: true, defaults: new List<ChannelBinding> { new() { ChannelType = ChannelType.Sms }, new() { ChannelType = ChannelType.WhatsApp } }));
        cut.Instance.UseSms.ShouldBeTrue();

        await Do(cut, w => w.SendPhoneCodeAsync());

        cut.Instance.PhoneVerified.ShouldBeFalse();
        cut.Instance.UseSms.ShouldBeFalse();
        cut.Instance.UseWhatsApp.ShouldBeFalse();
    }

    [Fact]
    public async Task Phone_SendFailure_KeepsTheCurrentState()
    {
        Api.Fail("POST /api/auth/phone/send-otp", "Teléfono no válido");
        var cut = RenderWizard(State(phoneVerified: true, defaults: new List<ChannelBinding> { new() { ChannelType = ChannelType.Sms } }));

        await Do(cut, w => w.SendPhoneCodeAsync());

        cut.Instance.PhoneCodeSent.ShouldBeFalse();
        cut.Instance.PhoneVerified.ShouldBeTrue();
        cut.Instance.UseSms.ShouldBeTrue();
        cut.Instance.PhoneMessage.ShouldBe("Teléfono no válido");
    }

    [Fact]
    public async Task TestChannel_SendsTheUrl_AndShowsTheResult()
    {
        Api.On("POST /api/onboarding/channels/test", new ChannelTestResultDto(true, "Prueba enviada por Slack. Comprueba que te ha llegado."));
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Channels);
        cut.Instance.Urls[ChannelType.Slack] = " https://hooks.slack.com/x ";

        await Do(cut, w => w.TestChannelAsync(ChannelType.Slack));
        cut.Render();

        var sent = Api.LastBody<ChannelTestRequest>("POST /api/onboarding/channels/test")!.Binding;
        sent.ChannelType.ShouldBe(ChannelType.Slack);
        sent.DestinationOverride.ShouldBe("https://hooks.slack.com/x");
        cut.Find("p.text-success").TextContent.ShouldBe("Prueba enviada por Slack. Comprueba que te ha llegado.");
    }

    [Fact]
    public async Task TestChannel_Email_HasNoDestination_AndFailuresAreShown()
    {
        Api.Fail("POST /api/onboarding/channels/test", "Demasiadas pruebas", HttpStatusCode.TooManyRequests);
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Channels);

        await Do(cut, w => w.TestChannelAsync(ChannelType.Email));
        cut.Render();

        Api.LastBody<ChannelTestRequest>("POST /api/onboarding/channels/test")!.Binding.DestinationOverride.ShouldBeNull();
        cut.Instance.TestResults[ChannelType.Email].Success.ShouldBeFalse();
        cut.Instance.TestResults[ChannelType.Email].Message.ShouldBe("Demasiadas pruebas");
        cut.Find("span.text-danger").TextContent.ShouldBe("Demasiadas pruebas");
    }

    [Fact]
    public async Task TestChannel_EmptyUrl_SendsNoDestination()
    {
        Api.On("POST /api/onboarding/channels/test", new ChannelTestResultDto(false, "La URL de Webhook debe ser una dirección https:// válida."));
        var cut = RenderWizard();
        cut.Instance.Urls[ChannelType.Webhook] = "   ";
        await Do(cut, w => w.TestChannelAsync(ChannelType.Webhook));
        Api.LastBody<ChannelTestRequest>("POST /api/onboarding/channels/test")!.Binding.DestinationOverride.ShouldBeNull();
        cut.Instance.TestResults[ChannelType.Webhook].Success.ShouldBeFalse();
    }

    [Fact]
    public void ChannelTestButton_InvokesTheTest()
    {
        Api.On("POST /api/onboarding/channels/test", new ChannelTestResultDto(true, "Prueba enviada por Email. Comprueba que te ha llegado."));
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Channels);
        cut.Render();

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Enviar prueba").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Prueba enviada por Email."));
    }

    // ---- Paso 3 ----

    [Theory]
    [InlineData(3600, false)]
    [InlineData(60, true)]
    public void FirstWatch_PresetsFollowThePlan(int minInterval, bool minutesOffered)
    {
        Api.On("POST /api/ping-tests", Draft());
        var cut = RenderWizard(State(minInterval: minInterval));
        cut.Instance.GoTo(Step.FirstWatch);
        cut.Render();

        var labels = cut.FindAll("#wk-interval option").Select(o => o.TextContent).ToList();
        labels.Contains("Cada 5 minutos").ShouldBe(minutesOffered);
        labels.ShouldContain("Cada hora");
        labels.ShouldContain("Cada semana");
        cut.Instance.IntervalSeconds.ShouldBe(86400);
        cut.Instance.GraceMinutes.ShouldBe(30);
    }

    [Fact]
    public async Task FirstWatch_RequiresAName()
    {
        var cut = RenderWizard();
        cut.Instance.WatchType = WatchType.Manual;
        cut.Instance.WatchName = "  ";
        await Do(cut, w => w.CreateWatchAsync());
        cut.Instance.Error.ShouldBe("Ponle un nombre a tu monitor.");
        Api.Count("POST /api/watches").ShouldBe(0);
    }

    [Fact]
    public async Task FirstWatch_Ping_RehearsesTheUrl_AndCreatesTheMonitorWithIt()
    {
        Api.On("POST /api/ping-tests", Draft());
        Api.On("POST /api/watches", CreatedWatch(WatchType.Ping, "tok123"), HttpStatusCode.Created);
        Api.On("GET /api/watches/w1/checkins", Array.Empty<CheckInDto>());
        var cut = RenderWizard(State(tz: "America/New_York"));
        cut.Instance.GoTo(Step.FirstWatch);
        cut.Render();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("https://www.wardkitten.test/p/tok123"));
        cut.Markup.ShouldContain("Esperando la primera llamada… abre la URL en tu navegador o llámala desde tu aplicación.");
        cut.Instance.WatchName = "  Backup  ";
        cut.Instance.IntervalSeconds = 21600;
        cut.Instance.GraceMinutes = 45;

        await Do(cut, w => w.CreateWatchAsync());

        var req = Api.LastBody<WatchRequest>("POST /api/watches")!;
        req.Name.ShouldBe("Backup");
        req.Type.ShouldBe(WatchType.Ping);
        req.PingProbeId.ShouldBe("probe-1");
        req.ChannelBindings.ShouldBeEmpty();               // el servidor aplica los canales por defecto
        req.Schedule.Kind.ShouldBe(ScheduleKind.Interval);
        req.Schedule.IntervalSeconds.ShouldBe(21600);
        req.Schedule.TimeZoneId.ShouldBe("America/New_York");
        req.Tolerance.GraceSeconds.ShouldBe(45 * 60);
        req.Severity.ShouldBe(Severity.Medium);

        cut.Instance.CreatedWatch!.Id.ShouldBe("w1");
        cut.Render();
        cut.Markup.ShouldContain("✅ Monitor creado. Las próximas llamadas a esta URL contarán como check-ins.");
        Api.Count("GET /api/watches/w1/checkins").ShouldBe(1);   // la tabla pasa a enseñar check-ins reales
        cut.FindAll("#wk-watch-name").ShouldBeEmpty();

        cut.FindAll("button").Single(b => b.TextContent == "Continuar").Click();
        cut.Instance.Step.ShouldBe(Step.Done);
        cut.Markup.ShouldContain("Tu primer monitor: <strong>Backup</strong>");
        Api.Count("DELETE /api/ping-tests/probe-1").ShouldBe(0);   // la URL la adoptó el monitor
    }

    [Fact]
    public async Task FirstWatch_Manual_HasNoProbe_AndNegativeGraceIsZero()
    {
        Api.On("POST /api/watches", CreatedWatch(WatchType.Manual, null), HttpStatusCode.Created);
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.FirstWatch);
        cut.Instance.WatchType = WatchType.Manual;
        cut.Render();
        cut.Instance.WatchName = "Regar";
        cut.Instance.GraceMinutes = -5;

        await Do(cut, w => w.CreateWatchAsync());
        cut.Render();

        var req = Api.LastBody<WatchRequest>("POST /api/watches")!;
        req.Type.ShouldBe(WatchType.Manual);
        req.PingProbeId.ShouldBeNull();
        req.Tolerance.GraceSeconds.ShouldBe(0);
        Api.Count("POST /api/ping-tests").ShouldBe(0);
        cut.Markup.ShouldContain("Monitor «Backup» creado");
    }

    [Fact]
    public async Task FirstWatch_ManualAfterRehearsingAPing_DoesNotReuseTheProbe()
    {
        Api.On("POST /api/ping-tests", Draft());
        Api.On("DELETE /api/ping-tests/probe-1", null, HttpStatusCode.NoContent);
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.FirstWatch);
        cut.Render();
        cut.WaitForAssertion(() => cut.Instance.Bench!.ProbeId.ShouldBe("probe-1"));
        cut.FindAll("input[type=radio]")[1].Change(true);
        cut.Instance.WatchName = "Regar";
        var busyWhileSaving = false;
        Api.On("POST /api/watches", _ =>
        {
            busyWhileSaving = cut.Instance.Busy;
            return FakeApi.Respond(CreatedWatch(WatchType.Manual, null), HttpStatusCode.Created);
        });

        await Do(cut, w => w.CreateWatchAsync());

        Api.LastBody<WatchRequest>("POST /api/watches")!.PingProbeId.ShouldBeNull();
        Api.Calls.ShouldNotContain(c => c.Key == "GET /api/watches/w1/checkins");
        busyWhileSaving.ShouldBeTrue();
    }

    [Fact]
    public async Task FirstWatch_CreateFailure_IsShown()
    {
        Api.On("POST /api/ping-tests", Draft());
        Api.Fail("POST /api/watches", "Has alcanzado el límite de 5 watches de tu plan.");
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.FirstWatch);
        cut.Render();
        cut.Instance.WatchName = "X";

        await Do(cut, w => w.CreateWatchAsync());

        cut.Instance.CreatedWatch.ShouldBeNull();
        cut.Instance.Busy.ShouldBeFalse();
        cut.Instance.Error.ShouldBe("Has alcanzado el límite de 5 watches de tu plan.");
    }

    [Fact]
    public void FirstWatch_SwitchingToManual_ClosesTheRehearsal()
    {
        Api.On("POST /api/ping-tests", Draft());
        Api.On("DELETE /api/ping-tests/probe-1", null, HttpStatusCode.NoContent);
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.FirstWatch);
        cut.Render();
        cut.WaitForAssertion(() => cut.Instance.Bench!.ProbeId.ShouldBe("probe-1"));

        cut.FindAll("input[type=radio]")[1].Change(true);

        cut.Instance.WatchType.ShouldBe(WatchType.Manual);
        cut.WaitForAssertion(() => Api.Count("DELETE /api/ping-tests/probe-1").ShouldBe(1));

        cut.FindAll("input[type=radio]")[0].Change(true);
        cut.Instance.WatchType.ShouldBe(WatchType.Ping);
    }

    // ---- Resumen y cierre ----

    [Fact]
    public async Task Finish_CompletesAndGoesToTheDashboard()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/welcome");
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Done);
        cut.Render();

        var busyWhileSaving = false;
        Api.On("POST /api/onboarding/complete", _ =>
        {
            busyWhileSaving = cut.Instance.Busy;
            return FakeApi.Respond(null, HttpStatusCode.NoContent);
        });

        cut.FindAll("button").Single(b => b.TextContent == "Ir al panel").Click();

        cut.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/"));
        busyWhileSaving.ShouldBeTrue();
        cut.Instance.Busy.ShouldBeFalse();
        Api.Count("POST /api/onboarding/complete").ShouldBe(1);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Finish_Failure_StaysAndShowsTheError()
    {
        Api.Fail("POST /api/onboarding/complete", "Usuario no encontrado.", HttpStatusCode.NotFound);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/welcome");
        var cut = RenderWizard();
        cut.Instance.GoTo(Step.Done);

        await Do(cut, w => w.FinishAsync());
        cut.Render();

        cut.Find(".alert-danger").TextContent.ShouldBe("Usuario no encontrado.");
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe("http://localhost/welcome");   // no navegó
        cut.Instance.Busy.ShouldBeFalse();
    }

    [Fact]
    public void Progress_IsSharedWithTheLayout_AndCanGoBack()
    {
        Api.On("PUT /api/onboarding/profile", new { });
        var progress = new OnboardingProgress();
        var cut = RenderWizard(progress: progress);
        progress.Current.ShouldBe(0);

        cut.Instance.SkipStep();
        cut.Instance.SkipStep();
        progress.Current.ShouldBe(2);
        progress.Furthest.ShouldBe(2);

        cut.InvokeAsync(() => progress.RequestStepAsync(0));
        cut.WaitForAssertion(() => cut.Instance.Step.ShouldBe(Step.Profile));
        progress.Furthest.ShouldBe(2);

        cut.Instance.Dispose();
        cut.InvokeAsync(() => progress.RequestStepAsync(1));
        cut.Instance.Step.ShouldBe(Step.Profile);   // desuscrita al desmontar
    }

    [Theory]
    [InlineData(ChannelType.Email, "Email")]
    [InlineData(ChannelType.Telegram, "Telegram")]
    [InlineData(ChannelType.Push, "Push")]
    [InlineData(ChannelType.Sms, "SMS")]
    [InlineData(ChannelType.WhatsApp, "WhatsApp")]
    [InlineData(ChannelType.Webhook, "Webhook")]
    [InlineData(ChannelType.Slack, "Slack")]
    [InlineData(ChannelType.Discord, "Discord")]
    [InlineData(ChannelType.MicrosoftTeams, "Microsoft Teams")]
    [InlineData((ChannelType)77, "77")]
    public void ChannelLabel(ChannelType type, string expected) => Welcome.ChannelLabel(type).ShouldBe(expected);

    [Theory]
    [InlineData(ChannelType.Slack, "https://hooks.slack.com/services/…")]
    [InlineData(ChannelType.Discord, "https://discord.com/api/webhooks/…")]
    [InlineData(ChannelType.MicrosoftTeams, "https://…webhook.office.com/…")]
    [InlineData(ChannelType.Webhook, "https://tu-servidor.example.com/wardkitten")]
    public void UrlPlaceholder(ChannelType type, string expected) => Welcome.UrlPlaceholder(type).ShouldBe(expected);
}
