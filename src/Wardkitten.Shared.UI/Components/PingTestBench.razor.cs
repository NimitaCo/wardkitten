// Feature: F03.03 — banco de pruebas de la URL de ping (componente reutilizable)
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Wardkitten.Domain.Watches;
using Wardkitten.Shared.Contracts;
using Wardkitten.Shared.UI.Services;

namespace Wardkitten.Shared.UI.Components;

/// <summary>
/// Banco de pruebas de la URL de ping (F03.03): reserva la URL (borrador) o ensaya la ya guardada (dry-run),
/// enseña URL + copiar + ejemplo <c>curl</c> y la tabla en vivo de solicitudes, refrescando por polling (no
/// SignalR: en producción hay varias réplicas sin backplane). Lo usan el alta/edición de watches y el asistente
/// de bienvenida. Al desmontarse cierra la prueba salvo que el padre la haya adoptado (<see cref="Release"/> o
/// <see cref="AttachToWatchAsync"/>).
/// </summary>
public partial class PingTestBench : ComponentBase, IAsyncDisposable
{
    public const string DefaultWaitingText = "Esperando la primera solicitud…";

    [Inject] private WardkittenApiClient Api { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Vigilancia ya guardada (edición). Null en un alta.</summary>
    [Parameter] public string? WatchId { get; set; }

    /// <summary>Token de ping ya guardado de la vigilancia (su URL está en producción).</summary>
    [Parameter] public string? SavedPingToken { get; set; }

    /// <summary>Fin de un ensayo en curso según la vigilancia guardada.</summary>
    [Parameter] public DateTime? SavedTestModeUntilUtc { get; set; }

    /// <summary>Si es false no se pinta ni se refresca (p. ej. el watch no es de tipo Ping), pero conserva la prueba.</summary>
    [Parameter] public bool Active { get; set; } = true;

    /// <summary>Reservar la URL de prueba en cuanto se activa, si aún no hay URL guardada.</summary>
    [Parameter] public bool AutoStart { get; set; } = true;

    /// <summary>Refresco mientras hay prueba en curso.</summary>
    [Parameter] public TimeSpan TestPollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Refresco cuando solo se mira el histórico real de la vigilancia.</summary>
    [Parameter] public TimeSpan HistoryPollInterval { get; set; } = TimeSpan.FromSeconds(15);

    [Parameter] public string Title { get; set; } = "🧪 Comprobar que llegan las solicitudes";
    [Parameter] public string Subtitle { get; set; } = "Llama a esta URL desde tu sistema y verás aquí cada solicitud.";
    [Parameter] public string WaitingText { get; set; } = DefaultWaitingText;

    /// <summary>Historial desplegado de entrada (el asistente lo enseña abierto: es la «tabla en vivo»).</summary>
    [Parameter] public bool HistoryOpen { get; set; }

    internal PingTestStateDto? Test { get; private set; }
    internal List<PingTestHitDto> Hits { get; private set; } = new();
    internal DateTime? LastHit { get; private set; }
    internal DateTime? PendingTestModeUntil { get; private set; }
    internal bool Busy { get; private set; }
    internal bool Copied { get; private set; }
    internal string? Error { get; private set; }

    private string? _watchId;
    private string? _savedToken;
    private string? _watchIdParam;
    private string? _savedTokenParam;
    private bool _paramsApplied;
    private bool _historyPending;
    private CancellationTokenSource? _pollCts;

    /// <summary>Banco en curso: el padre lo manda como <c>PingProbeId</c> al guardar para adoptar la URL.</summary>
    public string? ProbeId => Test?.ProbeId;

    internal bool HasSavedUrl => !string.IsNullOrEmpty(_savedToken);

    internal string? PingUrl => Test?.Url
        ?? (HasSavedUrl ? $"{Api.BaseAddress?.ToString().TrimEnd('/')}/p/{_savedToken}" : null);

    protected override void OnParametersSet()
    {
        // Los datos de la vigilancia guardada llegan una vez (o cuando cambian); después manda el estado interno.
        if (!_paramsApplied || WatchId != _watchIdParam || SavedPingToken != _savedTokenParam)
        {
            _watchIdParam = WatchId;
            _savedTokenParam = SavedPingToken;
            _watchId = WatchId;
            _savedToken = SavedPingToken;
            PendingTestModeUntil = SavedTestModeUntilUtc;
            _paramsApplied = true;
            _historyPending = true;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_pollCts is null)
        {
            _pollCts = new CancellationTokenSource();
            _ = PollLoopAsync(_pollCts.Token);
        }

        if (_historyPending)
        {
            _historyPending = false;
            if (HasSavedUrl && !string.IsNullOrEmpty(_watchId)) await RefreshHistoryAsync();
        }

        if (Active && AutoStart && Test is null && !HasSavedUrl && !Busy) await StartTestAsync();
    }

    /// <summary>Abre la prueba: borrador si no hay URL guardada; dry-run sobre la URL real si la hay.</summary>
    public async Task StartTestAsync()
    {
        Busy = true;
        Error = null;
        var r = await Api.StartPingTestAsync(new StartPingTestRequest(_watchId));
        Busy = false;
        if (r.Ok && r.Value is not null) Apply(r.Value);
        else Error = r.Error;
    }

    /// <summary>Termina la prueba: el servidor cierra el dry-run y borra el banco.</summary>
    public async Task StopTestAsync()
    {
        Busy = true;
        var probeId = Test?.ProbeId;
        Test = null;
        PendingTestModeUntil = null;
        if (probeId is not null) await Api.StopPingTestAsync(probeId);
        Busy = false;
        if (HasSavedUrl && !string.IsNullOrEmpty(_watchId)) await RefreshHistoryAsync();
        else { Hits = new(); LastHit = null; }
    }

    /// <summary>La vigilancia se guardó y adoptó el banco: ya no hay que cerrarlo al salir.</summary>
    public void Release() => Test = null;

    /// <summary>
    /// La vigilancia se acaba de crear con esta URL: a partir de ahora se enseñan sus check-ins reales.
    /// </summary>
    public async Task AttachToWatchAsync(string watchId, string? pingToken)
    {
        _watchId = watchId;
        _savedToken = string.IsNullOrEmpty(pingToken) ? Test?.Token : pingToken;
        Test = null;
        PendingTestModeUntil = null;
        await RefreshHistoryAsync();
        StateHasChanged();
    }

    internal void Apply(PingTestStateDto state)
    {
        Test = state;
        Hits = state.Hits;
        LastHit = state.Hits.Count > 0 ? state.Hits[0].ReceivedAtUtc : state.LastHitAtUtc;
        PendingTestModeUntil = state.TestModeUntilUtc;
    }

    /// <summary>Sin prueba en curso el histórico son los check-ins reales de la vigilancia.</summary>
    internal async Task RefreshHistoryAsync()
    {
        if (string.IsNullOrEmpty(_watchId)) return;
        var r = await Api.GetCheckInsAsync(_watchId);
        if (!r.Ok || r.Value is null) return;
        Hits = r.Value.Select(c => new PingTestHitDto(c.ReceivedAtUtc, c.Kind, c.Source, true, null, null, null, null)).ToList();
        LastHit = Hits.Count > 0 ? Hits[0].ReceivedAtUtc : null;
    }

    /// <summary>Un ciclo de refresco. Devuelve si había algo que refrescar.</summary>
    internal async Task<bool> PollOnceAsync()
    {
        if (!Active) return false;
        if (Test is not null)
        {
            var r = await Api.GetPingTestAsync(Test.ProbeId);
            if (r.Ok && r.Value is not null) Apply(r.Value);
            else Test = null;                              // caducó: el TTL ya lo ha limpiado
            return true;
        }
        if (HasSavedUrl && !string.IsNullOrEmpty(_watchId))
        {
            await RefreshHistoryAsync();
            return true;
        }
        return false;
    }

    internal TimeSpan CurrentPollInterval => Test is not null ? TestPollInterval : HistoryPollInterval;

    private async Task PollLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(CurrentPollInterval, ct);
                if (await PollOnceAsync()) await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) { /* desmontado */ }
    }

    internal async Task CopyAsync(string url)
    {
        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", url);
            Copied = true;
            StateHasChanged();
            await Task.Delay(TimeSpan.FromSeconds(2));
            Copied = false;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            Error = "No se ha podido copiar; selecciona la URL y cópiala a mano.";
        }
    }

    internal static string Hora(DateTime utc) => utc.ToLocalTime().ToString("HH:mm:ss");

    internal static string Hace(DateTime utc, DateTime nowUtc)
    {
        var seconds = (nowUtc - utc).TotalSeconds;
        if (seconds < 60) return $"hace {Math.Max(0, (int)seconds)} s";
        if (seconds < 3600) return $"hace {(int)(seconds / 60)} min";
        return $"hace {(int)(seconds / 3600)} h";
    }

    internal static string Tipo(string kind) => kind switch
    {
        "Start" => "▶ inicio",
        "Fail" => "✖ fallo",
        _ => "✔ hecho",
    };

    public async ValueTask DisposeAsync()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();

        // Salir sin guardar cierra la prueba: el dry-run deja de suspender la vigilancia y el borrador se borra
        // (el TTL de Mongo es solo la red de seguridad si el navegador se cierra de golpe).
        if (Test is not null)
        {
            try { await Api.StopPingTestAsync(Test.ProbeId); } catch { /* el TTL lo limpiará */ }
        }
        GC.SuppressFinalize(this);
    }
}
