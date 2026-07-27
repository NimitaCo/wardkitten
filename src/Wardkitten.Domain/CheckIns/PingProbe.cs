// Feature: F03.03 — banco de pruebas de la URL de ping (dry-run en alta/edición)
using Wardkitten.Domain.Common;

namespace Wardkitten.Domain.CheckIns;

/// <summary>Modo del banco de pruebas de la URL de ping. Feature: F03.03.</summary>
public enum PingProbeMode
{
    /// <summary>
    /// La vigilancia todavía no tiene URL (alta sin guardar, o una manual que se está convirtiendo a ping).
    /// El token se genera aquí y <b>se adopta tal cual</b> al guardar: la URL de prueba es la definitiva.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Ensayo sobre una vigilancia ya guardada: durante la ventana de prueba su URL real deja de contar
    /// (no reinicia contadores ni resuelve incidentes) y tampoco se evalúan sus vencimientos.
    /// </summary>
    DryRun = 1,
}

/// <summary>Solicitud recibida en el banco de pruebas. Es informativa: nunca cuenta como check-in.</summary>
public sealed class PingProbeHit
{
    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>Verbo HTTP con el que llegó la solicitud (GET/POST).</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Variante de la URL usada (<c>/p/{token}</c>, <c>/start</c>, <c>/fail</c>).</summary>
    public CheckInKind Kind { get; set; } = CheckInKind.Success;

    public string? RemoteIp { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>Cuerpo de la solicitud recortado a <see cref="PingProbe.MaxPayloadChars"/> (ayuda a depurar).</summary>
    public string? Payload { get; set; }
}

/// <summary>
/// Banco de pruebas de una URL de ping: permite comprobar durante el alta o la edición que el sistema
/// remoto llega, sin que esas solicitudes cuenten. Se autodestruye (índice TTL sobre
/// <see cref="ExpiresAtUtc"/>) si el usuario abandona el proceso. Feature: F03.03.
/// </summary>
public sealed class PingProbe : Entity
{
    /// <summary>Solicitudes conservadas en el documento (las más recientes).</summary>
    public const int MaxHits = 50;

    /// <summary>Recorte del cuerpo guardado por solicitud.</summary>
    public const int MaxPayloadChars = 2048;

    /// <summary>Vida de un borrador sin actividad; se renueva mientras la pantalla siga abierta.</summary>
    public static readonly TimeSpan DraftLifetime = TimeSpan.FromHours(2);

    /// <summary>Duración por defecto y máxima de un ensayo sobre una vigilancia ya guardada.</summary>
    public static readonly TimeSpan DefaultDryRunWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxDryRunWindow = TimeSpan.FromHours(1);

    /// <summary>Margen que sobrevive el historial una vez terminada la ventana de ensayo.</summary>
    public static readonly TimeSpan DryRunHistoryGrace = TimeSpan.FromHours(1);

    public string UserId { get; set; } = string.Empty;

    /// <summary>Token de la URL probada. En <see cref="PingProbeMode.Draft"/> es el que heredará el watch.</summary>
    public string Token { get; set; } = string.Empty;

    public PingProbeMode Mode { get; set; } = PingProbeMode.Draft;

    /// <summary>Vigilancia asociada. Null mientras es un borrador sin guardar.</summary>
    public string? WatchId { get; set; }

    /// <summary>Instante en que Mongo lo borra solo (índice TTL). Ver <see cref="IsActive"/>.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastHitAtUtc { get; set; }

    /// <summary>Total de solicitudes recibidas (aunque <see cref="Hits"/> solo guarde las últimas).</summary>
    public int HitCount { get; set; }

    public List<PingProbeHit> Hits { get; set; } = new();

    /// <summary>
    /// ¿Sigue vigente? El TTL de Mongo barre cada ~60 s, así que la caducidad se comprueba también aquí:
    /// un banco caducado nunca acepta solicitudes aunque el documento aún exista.
    /// </summary>
    public bool IsActive(DateTime nowUtc) => nowUtc < ExpiresAtUtc;

    /// <summary>Registra una solicitud en memoria (el repositorio lo hace de forma atómica en Mongo).</summary>
    public void RegisterHit(PingProbeHit hit)
    {
        LastHitAtUtc = hit.ReceivedAtUtc;
        HitCount++;
        Hits.Add(hit);
        if (Hits.Count > MaxHits) Hits.RemoveRange(0, Hits.Count - MaxHits);
    }

    /// <summary>Recorta el cuerpo recibido al máximo permitido (null si viene vacío).</summary>
    public static string? TrimPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        return payload.Length <= MaxPayloadChars ? payload : payload[..MaxPayloadChars];
    }

    /// <summary>Acota la ventana de ensayo pedida por el usuario a los límites del dominio.</summary>
    public static TimeSpan ClampDryRunWindow(TimeSpan requested)
        => requested <= TimeSpan.Zero ? DefaultDryRunWindow
         : requested > MaxDryRunWindow ? MaxDryRunWindow
         : requested;
}
