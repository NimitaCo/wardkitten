// Feature: F01.04 — asistente de bienvenida (onboarding)
using Microsoft.AspNetCore.Components;
using Wardkitten.Domain.Watches;
using Wardkitten.Shared.Contracts;
using Wardkitten.Shared.UI.Auth;
using Wardkitten.Shared.UI.Components;
using Wardkitten.Shared.UI.Services;
using Wardkitten.Web.Onboarding;

namespace Wardkitten.Web.Pages;

/// <summary>
/// Asistente de bienvenida (F01.04) para cuentas nuevas: 1) perfil, 2) cómo avisarte (canales por defecto,
/// F02.05, con verificación de email/teléfono, vinculación de Telegram F05.05 y envíos de prueba F05.06),
/// 3) primer monitor (con el banco de pruebas de la URL de ping, F03.03) y 4) resumen. Cada paso se puede
/// saltar explicando la consecuencia; «Saltar todo» (en <c>OnboardingLayout</c>) cierra el asistente y lleva al panel.
/// </summary>
public partial class Welcome : ComponentBase, IDisposable
{
    public enum WizardStep
    {
        Profile = 0,
        Channels = 1,
        FirstWatch = 2,
        Done = 3,
    }

    /// <summary>Integraciones salientes que se configuran con una URL, en el orden en que se muestran.</summary>
    internal static readonly IReadOnlyList<ChannelType> UrlChannels = new[]
    {
        ChannelType.Slack, ChannelType.Discord, ChannelType.MicrosoftTeams, ChannelType.Webhook,
    };

    [Inject] private WardkittenApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [CascadingParameter] public OnboardingProgress? Progress { get; set; }

    /// <summary>Cada cuánto se consulta si Telegram ya está vinculado mientras el usuario está en el bot.</summary>
    [Parameter] public TimeSpan TelegramPollInterval { get; set; } = TimeSpan.FromSeconds(3);

    internal WizardStep Step { get; private set; } = WizardStep.Profile;
    internal bool Loading { get; private set; } = true;
    internal bool Busy { get; private set; }
    internal string? Error { get; private set; }
    internal OnboardingStateDto? State { get; private set; }

    // ---- Paso 1: perfil ----
    internal string DisplayName { get; set; } = string.Empty;
    internal string TimeZoneId { get; set; } = string.Empty;
    internal string Locale { get; set; } = BrowserLocale.Default;
    internal List<TimeZoneOption> TimeZones { get; private set; } = new();

    // ---- Paso 2: canales ----
    internal bool UseEmail { get; set; } = true;
    internal bool EmailVerified { get; private set; }
    internal bool EmailCodeSent { get; private set; }
    internal string EmailCode { get; set; } = string.Empty;
    internal string? EmailMessage { get; private set; }

    internal bool TelegramAvailable { get; private set; }
    internal bool TelegramLinked { get; private set; }
    internal bool UseTelegram { get; set; }
    internal TelegramLinkCodeDto? TelegramLink { get; private set; }
    internal string? TelegramMessage { get; private set; }

    internal Dictionary<ChannelType, string> Urls { get; } = UrlChannels.ToDictionary(t => t, _ => string.Empty);
    internal Dictionary<ChannelType, ChannelTestResultDto> TestResults { get; } = new();

    internal string Phone { get; set; } = string.Empty;
    internal bool PhoneCodeSent { get; private set; }
    internal bool PhoneVerified { get; private set; }
    internal string PhoneCode { get; set; } = string.Empty;
    internal string? PhoneMessage { get; private set; }
    internal bool UseSms { get; set; }
    internal bool UseWhatsApp { get; set; }

    /// <summary>Canales por defecto guardados (vacío = los monitores nuevos avisan solo por email).</summary>
    internal List<ChannelBinding> SavedChannels { get; private set; } = new();

    // ---- Paso 3: primer monitor ----
    internal string WatchName { get; set; } = string.Empty;
    internal WatchType WatchType { get; set; } = WatchType.Ping;
    internal int IntervalSeconds { get; set; } = IntervalPresets.DefaultSeconds;
    internal int GraceMinutes { get; set; } = 30;
    internal IReadOnlyList<IntervalPreset> Presets { get; private set; } = IntervalPresets.All;
    internal WatchDto? CreatedWatch { get; private set; }

    /// <summary>Banco de pruebas de la URL (solo con tipo Ping). Lo asigna el marcado con <c>@ref</c>.</summary>
    internal PingTestBench? Bench { get; set; }

    private CancellationTokenSource? _telegramPoll;

    protected override async Task OnInitializedAsync()
    {
        if (Progress is not null) Progress.StepRequested += OnStepRequestedAsync;

        var r = await Api.GetOnboardingStateAsync();
        Loading = false;
        if (!r.Ok || r.Value is null)
        {
            Error = r.Error ?? "No se ha podido cargar tu cuenta.";
            return;
        }

        Load(r.Value);
        GoTo(WizardStep.Profile);
    }

    internal void Load(OnboardingStateDto state)
    {
        State = state;
        DisplayName = state.DisplayName;
        Locale = BrowserLocale.Normalize(state.Locale);
        TimeZones = TimeZoneOptions.Build(TimeZoneInfo.GetSystemTimeZones());
        TimeZoneId = TimeZoneOptions.Pick(TimeZones, state.TimeZoneId, TimeZoneInfo.Local.Id);

        EmailVerified = state.EmailVerified;
        TelegramAvailable = state.TelegramAvailable;
        TelegramLinked = state.TelegramLinked;
        Phone = state.Phone ?? string.Empty;
        PhoneVerified = state.PhoneVerified;

        Presets = IntervalPresets.For(state.MinIntervalSeconds);
        IntervalSeconds = IntervalPresets.Pick(Presets, IntervalSeconds);

        SavedChannels = state.DefaultChannels;
        if (state.DefaultChannels.Count > 0)
        {
            bool Has(ChannelType t) => state.DefaultChannels.Any(b => b.ChannelType == t);
            UseEmail = Has(ChannelType.Email);
            UseTelegram = TelegramLinked && Has(ChannelType.Telegram);
            UseSms = PhoneVerified && Has(ChannelType.Sms);
            UseWhatsApp = PhoneVerified && Has(ChannelType.WhatsApp);
            foreach (var type in UrlChannels)
                Urls[type] = state.DefaultChannels.FirstOrDefault(b => b.ChannelType == type)?.DestinationOverride ?? string.Empty;
        }
    }

    // ---- Navegación ----

    internal void GoTo(WizardStep step)
    {
        Step = step;
        Error = null;
        Progress?.Set((int)step);
    }

    private async Task OnStepRequestedAsync(int step)
    {
        GoTo((WizardStep)step);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>«Saltar este paso»: avanza sin guardar nada del paso actual.</summary>
    internal void SkipStep()
    {
        if (Step < WizardStep.Done) GoTo(Step + 1);
    }

    /// <summary>«Ir al panel» desde el resumen.</summary>
    internal async Task FinishAsync()
    {
        Busy = true;
        var r = await Api.CompleteOnboardingAsync();
        Busy = false;
        if (!r.Ok) { Error = r.Error; return; }
        Nav.NavigateTo("/");
    }

    // ---- Paso 1 ----

    internal async Task ContinueProfileAsync()
    {
        Busy = true;
        Error = null;
        var r = await Api.UpdateProfileAsync(new UpdateProfileRequest(DisplayName, TimeZoneId, Locale));
        Busy = false;
        if (!r.Ok) { Error = r.Error; return; }
        GoTo(WizardStep.Channels);
    }

    // ---- Paso 2 ----

    /// <summary>Canales elegidos, en orden de aviso. Solo entran los que ya pueden entregar.</summary>
    internal List<ChannelBinding> BuildDefaultBindings()
    {
        var list = new List<ChannelBinding>();
        void Add(bool on, ChannelType type, string? destination = null)
        {
            if (on) list.Add(new ChannelBinding { ChannelType = type, Enabled = true, Order = list.Count, DestinationOverride = destination });
        }

        Add(UseEmail, ChannelType.Email);
        Add(UseTelegram && TelegramLinked, ChannelType.Telegram);
        foreach (var type in UrlChannels)
        {
            var url = Urls[type].Trim();
            Add(url.Length > 0, type, url);
        }
        Add(UseSms && PhoneVerified, ChannelType.Sms);
        Add(UseWhatsApp && PhoneVerified, ChannelType.WhatsApp);
        return list;
    }

    internal async Task ContinueChannelsAsync()
    {
        var bindings = BuildDefaultBindings();
        if (bindings.Count == 0)
        {
            Error = "Elige al menos un canal: sin ninguno no podríamos avisarte.";
            return;
        }

        Busy = true;
        Error = null;
        var r = await Api.UpdateDefaultChannelsAsync(new UpdateChannelsRequest(bindings));
        Busy = false;
        if (!r.Ok) { Error = r.Error; return; }
        SavedChannels = r.Value ?? bindings;
        GoTo(WizardStep.FirstWatch);
    }

    internal async Task SendEmailCodeAsync()
    {
        var r = await Api.SendEmailCodeAsync();
        EmailCodeSent = r.Ok;
        EmailMessage = r.Ok ? $"Te hemos enviado un código a {State?.Email}." : r.Error;
    }

    internal async Task VerifyEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(EmailCode)) { EmailMessage = "Escribe el código que te hemos enviado."; return; }
        var r = await Api.VerifyEmailAsync(EmailCode.Trim());
        if (!r.Ok) { EmailMessage = r.Error; return; }
        EmailVerified = true;
        EmailMessage = "✅ Email verificado.";
    }

    internal async Task StartTelegramLinkAsync()
    {
        TelegramMessage = null;
        var r = await Api.CreateTelegramLinkCodeAsync();
        if (!r.Ok || r.Value is null) { TelegramMessage = r.Error; return; }
        TelegramLink = r.Value;

        _telegramPoll?.Cancel();
        _telegramPoll = new CancellationTokenSource();
        _ = PollTelegramAsync(_telegramPoll.Token);
    }

    /// <summary>Consulta si ya se pulsó «Start» en el bot. Devuelve true al quedar vinculado.</summary>
    internal async Task<bool> CheckTelegramStatusAsync()
    {
        var r = await Api.GetTelegramStatusAsync();
        if (!r.Ok || r.Value is not { Linked: true }) return false;
        TelegramLinked = true;
        UseTelegram = true;
        TelegramLink = null;
        TelegramMessage = "✅ Telegram vinculado.";
        return true;   // el bucle de consulta termina al recibir true
    }

    private async Task PollTelegramAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TelegramPollInterval, ct);
                if (await CheckTelegramStatusAsync())
                {
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }
        }
        catch (OperationCanceledException) { /* vinculado, desvinculado o salió de la página */ }
    }

    internal async Task UnlinkTelegramAsync()
    {
        _telegramPoll?.Cancel();
        var r = await Api.UnlinkTelegramAsync();
        if (!r.Ok) { TelegramMessage = r.Error; return; }
        TelegramLinked = false;
        UseTelegram = false;
        TelegramLink = null;
        TelegramMessage = null;
    }

    internal async Task SendPhoneCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Phone)) { PhoneMessage = "Escribe tu número de teléfono."; return; }
        var r = await Api.SendPhoneOtpAsync(Phone.Trim());
        PhoneCodeSent = r.Ok;
        if (r.Ok) { PhoneVerified = false; UseSms = false; UseWhatsApp = false; }
        PhoneMessage = r.Ok ? "Te hemos enviado un SMS con un código." : r.Error;
    }

    internal async Task VerifyPhoneAsync()
    {
        if (string.IsNullOrWhiteSpace(PhoneCode)) { PhoneMessage = "Escribe el código del SMS."; return; }
        var r = await Api.VerifyPhoneAsync(PhoneCode.Trim());
        if (!r.Ok) { PhoneMessage = r.Error; return; }
        PhoneVerified = true;
        PhoneCodeSent = false;
        PhoneMessage = "✅ Teléfono verificado.";
    }

    /// <summary>«Enviar prueba» por un canal, al mismo destino al que irán las alertas.</summary>
    internal async Task TestChannelAsync(ChannelType type)
    {
        var url = Urls.TryGetValue(type, out var u) ? u.Trim() : string.Empty;
        var binding = new ChannelBinding { ChannelType = type, DestinationOverride = url.Length > 0 ? url : null };
        var r = await Api.TestChannelAsync(new ChannelTestRequest(binding));
        TestResults[type] = r.Ok && r.Value is not null
            ? r.Value
            : new ChannelTestResultDto(false, r.Error ?? "No se ha podido enviar la prueba.");
    }

    // ---- Paso 3 ----

    internal async Task CreateWatchAsync()
    {
        if (string.IsNullOrWhiteSpace(WatchName)) { Error = "Ponle un nombre a tu monitor."; return; }

        var request = new WatchRequest(
            WatchName.Trim(), null, WatchType,
            new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = IntervalSeconds, TimeZoneId = TimeZoneId },
            new Tolerance { GraceSeconds = Math.Max(0, GraceMinutes) * 60 },
            new List<ChannelBinding>(),     // sin bindings: el servidor aplica los canales por defecto (F02.05)
            Severity.Medium, null, null, null, 0,
            WatchType == WatchType.Ping ? Bench?.ProbeId : null);   // adopta la URL ensayada (F03.03)

        Busy = true;
        Error = null;
        var r = await Api.CreateWatchAsync(request);
        Busy = false;
        if (!r.Ok || r.Value is null) { Error = r.Error; return; }

        CreatedWatch = r.Value;
        if (CreatedWatch.Type == WatchType.Ping && Bench is not null)
            await Bench.AttachToWatchAsync(CreatedWatch.Id, CreatedWatch.PingToken);
    }

    // ---- Resumen ----

    internal IReadOnlyList<string> ChannelSummary => SavedChannels.Count == 0
        ? new[] { "Email (por defecto)" }
        : SavedChannels.Select(b => ChannelLabel(b.ChannelType)).ToList();

    internal static string ChannelLabel(ChannelType type) => type switch
    {
        ChannelType.Email => "Email",
        ChannelType.Telegram => "Telegram",
        ChannelType.Push => "Push",
        ChannelType.Sms => "SMS",
        ChannelType.WhatsApp => "WhatsApp",
        ChannelType.Webhook => "Webhook",
        ChannelType.Slack => "Slack",
        ChannelType.Discord => "Discord",
        ChannelType.MicrosoftTeams => "Microsoft Teams",
        _ => type.ToString(),
    };

    internal static string UrlPlaceholder(ChannelType type) => type switch
    {
        ChannelType.Slack => "https://hooks.slack.com/services/…",
        ChannelType.Discord => "https://discord.com/api/webhooks/…",
        ChannelType.MicrosoftTeams => "https://…webhook.office.com/…",
        _ => "https://tu-servidor.example.com/wardkitten",
    };

    public void Dispose()
    {
        if (Progress is not null) Progress.StepRequested -= OnStepRequestedAsync;
        _telegramPoll?.Cancel();
        _telegramPoll?.Dispose();
        GC.SuppressFinalize(this);
    }
}
