// Feature: F01.04 — asistente de bienvenida (periodicidades en lenguaje humano)
namespace Wardkitten.Web.Onboarding;

public sealed record IntervalPreset(int Seconds, string Label);

/// <summary>
/// Periodicidades que ofrece el asistente en lugar de «cada N segundos». Se filtran por el mínimo del plan
/// (<c>PlanLimits.MinIntervalSeconds</c>): en Free no aparecen las de minutos. Para cualquier otra
/// periodicidad (cron, intervalos a medida) está la pantalla completa de alta.
/// </summary>
public static class IntervalPresets
{
    public const int DefaultSeconds = 86400;

    // HARDCODE (ver HARDCODED.md): catálogo de periodicidades del asistente de bienvenida.
    public static readonly IReadOnlyList<IntervalPreset> All = new IntervalPreset[]
    {
        new(300, "Cada 5 minutos"),
        new(900, "Cada 15 minutos"),
        new(1800, "Cada 30 minutos"),
        new(3600, "Cada hora"),
        new(21600, "Cada 6 horas"),
        new(43200, "Cada 12 horas"),
        new(86400, "Cada día"),
        new(604800, "Cada semana"),
    };

    /// <summary>Las que permite el plan (intervalo ≥ mínimo del plan).</summary>
    public static IReadOnlyList<IntervalPreset> For(int minIntervalSeconds)
        => All.Where(p => p.Seconds >= minIntervalSeconds).ToList();

    /// <summary>La preferida si el plan la permite; si no, la más cercana permitida (o la diaria si no hay ninguna).</summary>
    public static int Pick(IReadOnlyList<IntervalPreset> allowed, int preferredSeconds)
    {
        if (allowed.Count == 0) return DefaultSeconds;
        if (allowed.Any(p => p.Seconds == preferredSeconds)) return preferredSeconds;
        return allowed.OrderBy(p => Math.Abs(p.Seconds - preferredSeconds)).First().Seconds;
    }
}
