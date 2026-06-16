namespace Wardkitten.Shared.Contracts;

public sealed record TeamDto(
    string Id,
    string Name,
    string OwnerId,
    List<TeamMemberDto> Members,
    OnCallDto? OnCall,
    string? CurrentOnCallUserId);

public sealed record TeamMemberDto(string UserId, string Email, string DisplayName, bool IsOwner);

public sealed record OnCallDto(DateTime AnchorUtc, int ShiftSeconds, List<string> RotationUserIds, List<OnCallOverrideDto> Overrides);

public sealed record OnCallOverrideDto(DateTime StartUtc, DateTime EndUtc, string UserId);

public sealed record CreateTeamRequest(string Name);

public sealed record AddMemberRequest(string Email);

public sealed record SetOnCallRequest(DateTime AnchorUtc, int ShiftSeconds, List<string> RotationUserIds);

public sealed record AddOnCallOverrideRequest(DateTime StartUtc, DateTime EndUtc, string UserId);
