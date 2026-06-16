namespace Wardkitten.Shared.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string? TimeZoneId, string? Locale);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(string AccessToken, DateTime AccessExpiresUtc, string RefreshToken, UserDto User);

public sealed record UserDto(
    string Id,
    string Email,
    string DisplayName,
    string TimeZoneId,
    string Locale,
    string Plan,
    bool EmailVerified,
    bool PhoneVerified,
    string? Phone,
    IReadOnlyList<string> Roles);

public sealed record VerifyCodeRequest(string Code);

public sealed record PhoneOtpRequest(string Phone);

public sealed record PushTokenRequest(string Token);
