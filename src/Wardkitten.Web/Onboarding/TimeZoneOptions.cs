// Feature: F01.04 — asistente de bienvenida (selector de zona horaria)
namespace Wardkitten.Web.Onboarding;

public sealed record TimeZoneOption(string Id, string Label);

/// <summary>
/// Opciones del selector de zona horaria a partir de <see cref="TimeZoneInfo.GetSystemTimeZones()"/> (en el
/// navegador son ids IANA, los mismos que entiende el servidor Linux), ordenadas por desfase.
/// </summary>
public static class TimeZoneOptions
{
    public static List<TimeZoneOption> Build(IEnumerable<TimeZoneInfo> zones)
        => zones
            .OrderBy(z => z.BaseUtcOffset)
            .ThenBy(z => z.Id, StringComparer.Ordinal)
            .Select(z => new TimeZoneOption(z.Id, $"(UTC{Offset(z.BaseUtcOffset)}) {z.Id.Replace('_', ' ')}"))
            .ToList();

    internal static string Offset(TimeSpan offset)
        => (offset < TimeSpan.Zero ? "-" : "+") + offset.Duration().ToString(@"hh\:mm");

    /// <summary>
    /// Zona a preseleccionar: la guardada en la cuenta (el registro guarda la del navegador, así que para una
    /// cuenta nueva es la del navegador) y, si la cuenta no tiene, la del navegador. Si la elegida no está en
    /// la lista (p. ej. ids con otro formato) se añade al principio para no perderla.
    /// </summary>
    public static string Pick(List<TimeZoneOption> options, string? saved, string? browser)
    {
        var chosen = !string.IsNullOrWhiteSpace(saved) ? saved.Trim()
            : !string.IsNullOrWhiteSpace(browser) ? browser.Trim()
            : "UTC";
        if (!options.Any(o => o.Id == chosen)) options.Insert(0, new TimeZoneOption(chosen, chosen));
        return chosen;
    }
}
