using System.Globalization;

namespace Wardkitten.Shared.UI.Auth;

/// <summary>
/// Idioma de la interfaz a partir del del navegador (en Blazor WASM la cultura actual es la del navegador).
/// Solo hay dos idiomas soportados; cualquier otro cae en español, el idioma por defecto. Los mismos que
/// acepta el servidor en <c>OnboardingService.SupportedLocales</c>.
/// </summary>
public static class BrowserLocale
{
    public const string Default = "es";

    public static string Current() => Normalize(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    public static string Normalize(string? twoLetter)
        => string.Equals(twoLetter?.Trim(), "en", StringComparison.OrdinalIgnoreCase) ? "en" : Default;
}
