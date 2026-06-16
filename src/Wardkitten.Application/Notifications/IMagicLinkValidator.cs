namespace Wardkitten.Application.Notifications;

/// <summary>Datos extraídos de un magic link válido (acción ACK/Hecho/Snooze).</summary>
public sealed record MagicLinkData(string IncidentId, string WatchId, string Action);

/// <summary>Valida los magic links firmados generados por <see cref="IAckLinkBuilder"/>. Ver SECURITY.md.</summary>
public interface IMagicLinkValidator
{
    /// <summary>Devuelve los datos si el token es válido (firma + expiración), o null si no.</summary>
    MagicLinkData? Validate(string token);
}
