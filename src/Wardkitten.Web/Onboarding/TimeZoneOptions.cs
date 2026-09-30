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
    /// Zona a preseleccionar: la guardada en la cuenta (el registro ya guarda la del navegador) y, si no
    /// existe en la lista, la del navegador. Si ninguna está en la lista se añade la elegida para no perderla.
    /// </summary>
    public static string Pick(List<TimeZoneOption> options, string? saved, string? browser)
    {
        foreach (var candidate in new[] { saved, browser })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && options.Any(o => o.Id == candidate)) return candidate!;
        }

        var chosen = !string.IsNullOrWhiteSpace(saved) ? saved! : !string.IsNullOrWhiteSpace(browser) ? browser! : "UTC";
        options.Insert(0, new TimeZoneOption(chosen, chosen));
        return chosen;
    }
}
