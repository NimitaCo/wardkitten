using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

public sealed record WatchTemplate(string Id, string Name, string Description, string Emoji);

/// <summary>Plantillas predefinidas para crear watches comunes con un clic. Feature: F02.04.</summary>
public static class WatchTemplateCatalog
{
    public static IReadOnlyList<WatchTemplate> All { get; } = new[]
    {
        new WatchTemplate("backup-diario", "Backup diario", "Un proceso hace ping a una URL al terminar el backup", "💾"),
        new WatchTemplate("facturacion-mensual", "Facturación mensual", "Recuérdame facturar el día 1 de cada mes", "🧾"),
        new WatchTemplate("regar-plantas", "Regar las plantas", "Cada 3 días, con margen y tolerancia a un olvido", "🪴"),
        new WatchTemplate("cambiar-filtros", "Cambiar filtros", "Mantenimiento mensual", "🧹"),
        new WatchTemplate("revisar-logs", "Revisar logs", "Cada día laborable a las 9:00", "📊"),
    };

    public static WatchInput? BuildInput(string id) => id switch
    {
        "backup-diario" => new WatchInput(
            "Backup diario", "Ping al terminar el backup", WatchType.Ping,
            new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 86400 },
            new Tolerance { GraceSeconds = 7200, SkipTolerance = 0 },
            Channels(ChannelType.Email, ChannelType.Telegram), Severity.High, new() { "backup" }, null),

        "facturacion-mensual" => new WatchInput(
            "Facturación mensual", "Facturar el día 1", WatchType.Manual,
            new Schedule { Kind = ScheduleKind.Cron, CronExpression = "0 9 1 * *" },
            new Tolerance { GraceSeconds = 172800, SkipTolerance = 0 },
            Channels(ChannelType.Email), Severity.High, new() { "facturacion" }, null),

        "regar-plantas" => new WatchInput(
            "Regar las plantas", "Cada 3 días", WatchType.Manual,
            new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 259200 },
            new Tolerance { GraceSeconds = 43200, SkipTolerance = 1 },
            Channels(ChannelType.Push), Severity.Low, new() { "casa" }, null),

        "cambiar-filtros" => new WatchInput(
            "Cambiar filtros", "Mantenimiento mensual", WatchType.Manual,
            new Schedule { Kind = ScheduleKind.Interval, IntervalSeconds = 2592000 },
            new Tolerance { GraceSeconds = 259200, SkipTolerance = 0 },
            Channels(ChannelType.Email), Severity.Medium, new() { "mantenimiento" }, null),

        "revisar-logs" => new WatchInput(
            "Revisar logs", "Días laborables a las 9:00", WatchType.Manual,
            new Schedule { Kind = ScheduleKind.Cron, CronExpression = "0 9 * * 1-5" },
            new Tolerance { GraceSeconds = 14400, SkipTolerance = 1 },
            Channels(ChannelType.Email), Severity.Medium, new() { "ops" }, null),

        _ => null,
    };

    private static List<ChannelBinding> Channels(params ChannelType[] types)
        => types.Select((t, i) => new ChannelBinding { ChannelType = t, Enabled = true, Order = i }).ToList();
}
