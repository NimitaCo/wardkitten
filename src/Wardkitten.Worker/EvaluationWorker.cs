using Es.Nimita.Infra.Mongo.Leasing;
using Wardkitten.Application.Evaluation;

namespace Wardkitten.Worker;

/// <summary>
/// Hospeda el motor de evaluación. Bajo <b>leader election</b> (lease en Mongo) solo una réplica evalúa,
/// evitando alertas duplicadas al escalar. Cada tick: barre watches vencidos, avanza el escalado de los
/// incidentes abiertos y emite un heartbeat externo (self-monitoring). Feature: F04.03.
/// </summary>
public sealed class EvaluationWorker : BackgroundService
{
    private const string LeaseResource = "evaluation-engine";

    private readonly ILeaseStore _lease;
    private readonly EvaluationEngine _engine;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<EvaluationWorker> _logger;

    private readonly string _holder = $"{Environment.MachineName}-{Guid.NewGuid().ToString("N")[..8]}";
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _leaseTtl;

    public EvaluationWorker(
        ILeaseStore lease,
        EvaluationEngine engine,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<EvaluationWorker> logger)
    {
        _lease = lease;
        _engine = engine;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;

        var seconds = int.TryParse(config["POLLING_INTERVAL_SECONDS"], out var s) && s > 0 ? s : 30;
        _pollInterval = TimeSpan.FromSeconds(seconds);
        _leaseTtl = TimeSpan.FromSeconds(Math.Max(seconds * 3, 60)); // sobrevive a varios ticks
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EvaluationWorker iniciado (holder={Holder}, intervalo={Interval}s)", _holder, _pollInterval.TotalSeconds);

        using var timer = new PeriodicTimer(_pollInterval);
        do
        {
            try
            {
                var isLeader = await _lease.TryAcquireAsync(LeaseResource, _holder, _leaseTtl, stoppingToken);
                if (!isLeader)
                {
                    _logger.LogDebug("No somos líder; otra réplica evalúa.");
                    continue;
                }

                var due = await _engine.EvaluateDueAsync(stoppingToken);
                var escalated = await _engine.ProcessOpenIncidentsAsync(stoppingToken);
                if (due > 0 || escalated > 0)
                    _logger.LogInformation("Tick: {Due} watches evaluados, {Escalated} incidentes escalados", due, escalated);

                await SelfPingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de evaluación");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));

        await _lease.ReleaseAsync(LeaseResource, _holder, CancellationToken.None);
        _logger.LogInformation("EvaluationWorker detenido; lease liberado.");
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    /// <summary>Heartbeat a un watchdog externo: ¿quién vigila al vigilante? (ver plan, self-monitoring).</summary>
    private async Task SelfPingAsync(CancellationToken ct)
    {
        var url = _config["SELFCHECK_PING_URL"];
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            var client = _httpFactory.CreateClient("selfcheck");
            using var response = await client.GetAsync(url, ct);
            _ = response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo en el self-ping de monitorización");
        }
    }
}
