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
    string? ProjectId);

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
    DateTime CreatedAtUtc);

public sealed record CheckInDto(string Id, string Kind, string Source, DateTime ReceivedAtUtc, int? DurationMs);

public sealed record WatchTemplateDto(string Id, string Name, string Description, string Emoji);
