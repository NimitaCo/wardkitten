using Wardkitten.Domain.Common;

namespace Wardkitten.Domain.CheckIns;

public enum CheckInKind
{
    /// <summary>La tarea/proceso se completó correctamente.</summary>
    Success = 0,

    /// <summary>Un proceso largo ha empezado (permite detectar jobs que arrancan y nunca acaban).</summary>
    Start = 1,

    /// <summary>La tarea/proceso reportó fallo explícito.</summary>
    Fail = 2,
}

public enum CheckInSource
{
    Http = 0,    // ping a la URL única
    App = 1,     // botón en web/móvil
    Telegram = 2,
    Email = 3,
    Sms = 4,
    System = 5,
}

/// <summary>
/// Señal de confirmación recibida para un watch. Feature: F03.01.
/// Se almacena en una colección normal indexada por <c>watchId</c> + <see cref="ReceivedAtUtc"/>
/// (antes era time-series, que obligaba a MongoDB 5.0+).
/// </summary>
public sealed class CheckIn : Entity
{
    public string WatchId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public CheckInKind Kind { get; set; } = CheckInKind.Success;
    public CheckInSource Source { get; set; } = CheckInSource.Http;

    public DateTime ReceivedAtUtc { get; set; }

    /// <summary>Datos opcionales del ping (código de salida, logs, métricas…).</summary>
    public string? Payload { get; set; }

    /// <summary>Duración del proceso si se reportó (par Start/Success).</summary>
    public int? DurationMs { get; set; }

    public string? RemoteIp { get; set; }
}
