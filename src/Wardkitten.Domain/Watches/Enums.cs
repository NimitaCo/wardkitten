namespace Wardkitten.Domain.Watches;

/// <summary>Cómo se confirma que una tarea se ha hecho.</summary>
public enum WatchType
{
    /// <summary>Un proceso automático hace ping HTTP a una URL única al terminar.</summary>
    Ping = 0,

    /// <summary>El usuario confirma manualmente (app, email, Telegram…).</summary>
    Manual = 1,
}

/// <summary>Estado del ciclo de vida de un watch.</summary>
public enum WatchStatus
{
    /// <summary>Recién creado, aún sin primer check-in.</summary>
    New = 0,

    /// <summary>Al día: el último check-in llegó en plazo.</summary>
    Up = 1,

    /// <summary>Vencido pero dentro de la tolerancia (gracia / skips restantes).</summary>
    Grace = 2,

    /// <summary>Incumplió la programación: hay un incidente abierto.</summary>
    Down = 3,

    /// <summary>Pausado por el usuario o en ventana de mantenimiento: no se evalúa.</summary>
    Paused = 4,
}

public enum Severity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}

/// <summary>Tipo de canal de notificación. Algunos son <em>metered</em> (consumen créditos).</summary>
public enum ChannelType
{
    Email = 0,
    Telegram = 1,
    Push = 2,
    Sms = 3,
    WhatsApp = 4,
    // Integraciones salientes (gratuitas). El destino es la URL del webhook (DestinationOverride).
    Webhook = 5,
    Slack = 6,
    Discord = 7,
    MicrosoftTeams = 8,
}

public static class ChannelTypeExtensions
{
    /// <summary>SMS y WhatsApp tienen coste real y se pagan con la wallet de créditos.</summary>
    public static bool IsMetered(this ChannelType type)
        => type is ChannelType.Sms or ChannelType.WhatsApp;
}

public enum ScheduleKind
{
    /// <summary>Cada N segundos desde el último check-in.</summary>
    Interval = 0,

    /// <summary>Expresión cron (5 campos) evaluada en la zona horaria del watch.</summary>
    Cron = 1,

    /// <summary>Lista explícita de fechas/horas locales.</summary>
    Calendar = 2,
}
