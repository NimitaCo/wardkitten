using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Shared.Contracts;

public sealed record WatchRequest(
    string Name,
    string? Description,
    WatchType Type,
    Schedule Schedule,
    Tolerance Tolerance,
    List<ChannelBinding> ChannelBindings,
    Severity Severity,
    List<string>? Tags,
    string? ProjectId,
    string? EscalationTeamId = null,
    int TeamEscalationDelaySeconds = 0,
    // Banco de pruebas usado al configurar la URL: la vigilancia adopta su token (F03.03).
    string? PingProbeId = null);

public sealed record WatchDto(
    string Id,
    string Name,
    string? Description,
    WatchType Type,
    Schedule Schedule,
    Tolerance Tolerance,
    List<ChannelBinding> ChannelBindings,
    Severity Severity,
    WatchStatus Status,
    bool Paused,
    DateTime? NextDueAtUtc,
    DateTime? LastCheckInAtUtc,
    int ConsecutiveMisses,
    string? PingToken,
    List<string> Tags,
    string? ProjectId,
    string? CurrentIncidentId,
    int CurrentStreak,
    int BestStreak,
    string? EscalationTeamId,
    int TeamEscalationDelaySeconds,
    DateTime CreatedAtUtc,
    DateTime? TestModeUntilUtc);

public sealed record CheckInDto(string Id, string Kind, string Source, DateTime ReceivedAtUtc, int? DurationMs);

public sealed record WatchTemplateDto(string Id, string Name, string Description, string Emoji);

// ---- Banco de pruebas de la URL de ping (F03.03) ----

/// <summary>
/// Abre una prueba de la URL. Sin <paramref name="WatchId"/> se reserva un token nuevo para el alta;
/// con una vigilancia que ya tiene URL se abre una ventana de ensayo de <paramref name="Minutes"/>.
/// </summary>
public sealed record StartPingTestRequest(string? WatchId = null, int? Minutes = null);

/// <summary>Solicitud llegada a la URL. <c>Counted = false</c> ⇒ era de prueba y no reinició nada.</summary>
public sealed record PingTestHitDto(
    DateTime ReceivedAtUtc,
    string Kind,
    string Source,
    bool Counted,
    string? Method,
    string? RemoteIp,
    string? UserAgent,
    string? Payload);

public sealed record PingTestStateDto(
    string ProbeId,
    string Token,
    string Url,
    PingProbeMode Mode,
    DateTime ExpiresAtUtc,
    DateTime? TestModeUntilUtc,
    DateTime? LastHitAtUtc,
    int HitCount,
    List<PingTestHitDto> Hits);
