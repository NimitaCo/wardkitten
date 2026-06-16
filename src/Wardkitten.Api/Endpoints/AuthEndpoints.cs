using System.Security.Claims;
using Wardkitten.Api.Mapping;
using Wardkitten.Api.Security;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Notifications;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Watches;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        g.MapPost("/register", async (RegisterRequest req, AuthService auth, CancellationToken ct) =>
        {
            var r = await auth.RegisterAsync(req.Email, req.Password, req.DisplayName, req.TimeZoneId, req.Locale, ct);
            return r.Success ? Results.Ok(ToResponse(r.Value!)) : Results.BadRequest(new { error = r.Error });
        });

        g.MapPost("/login", async (LoginRequest req, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            var r = await auth.LoginAsync(req.Email, req.Password, Ip(http), ct);
            return r.Success ? Results.Ok(ToResponse(r.Value!)) : Results.Json(new { error = r.Error }, statusCode: 401);
        }).RequireRateLimiting("auth");

        g.MapPost("/refresh", async (RefreshRequest req, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            var r = await auth.RefreshAsync(req.RefreshToken, Ip(http), ct);
            return r.Success ? Results.Ok(ToResponse(r.Value!)) : Results.Json(new { error = r.Error }, statusCode: 401);
        });

        g.MapPost("/logout", async (RefreshRequest req, AuthService auth, CancellationToken ct) =>
        {
            await auth.RevokeAsync(req.RefreshToken, ct);
            return Results.NoContent();
        }).RequireAuthorization();

        g.MapGet("/me", async (ClaimsPrincipal principal, IUserRepository users, CancellationToken ct) =>
        {
            var user = await users.GetByIdAsync(principal.UserId()!, ct);
            return user is null ? Results.NotFound() : Results.Ok(user.ToDto());
        }).RequireAuthorization();

        g.MapPost("/email/send-code", async (ClaimsPrincipal principal, AuthService auth, IUserRepository users,
            IEnumerable<INotificationChannel> channels, CancellationToken ct) =>
        {
            var userId = principal.UserId()!;
            var user = await users.GetByIdAsync(userId, ct);
            if (user is null) return Results.NotFound();
            var r = await auth.GenerateEmailVerificationCodeAsync(userId, ct);
            if (!r.Success) return Results.BadRequest(new { error = r.Error });
            await SendSystemMessageAsync(channels, ChannelType.Email, user.Email,
                "Wardkitten · verifica tu email", $"Tu código de verificación es: {r.Value}", ct);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("auth");

        g.MapPost("/email/verify", async (ClaimsPrincipal principal, VerifyCodeRequest req, AuthService auth, CancellationToken ct) =>
        {
            var r = await auth.VerifyEmailAsync(principal.UserId()!, req.Code, ct);
            return r.Success ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
        }).RequireAuthorization();

        g.MapPost("/phone/send-otp", async (ClaimsPrincipal principal, PhoneOtpRequest req, AuthService auth,
            IEnumerable<INotificationChannel> channels, CancellationToken ct) =>
        {
            // El OTP de teléfono se envía por SMS con coste sponsored (no consume wallet). Ver SECURITY.md.
            var r = await auth.GeneratePhoneOtpAsync(principal.UserId()!, req.Phone, ct);
            if (!r.Success) return Results.BadRequest(new { error = r.Error });
            await SendSystemMessageAsync(channels, ChannelType.Sms, req.Phone,
                "Wardkitten", $"Tu código de verificación es: {r.Value}", ct);
            return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("auth");

        g.MapPost("/phone/verify", async (ClaimsPrincipal principal, VerifyCodeRequest req, AuthService auth, CancellationToken ct) =>
        {
            var r = await auth.VerifyPhoneAsync(principal.UserId()!, req.Code, ct);
            return r.Success ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
        }).RequireAuthorization();

        // Registro del token de push (FCM) de un dispositivo móvil. Feature: F09.
        g.MapPost("/push-tokens", async (ClaimsPrincipal principal, PushTokenRequest req, IUserRepository users, CancellationToken ct) =>
        {
            var user = await users.GetByIdAsync(principal.UserId()!, ct);
            if (user is null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(req.Token) && !user.PushTokens.Contains(req.Token))
            {
                user.PushTokens.Add(req.Token);
                await users.ReplaceAsync(user, ct);
            }
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static AuthResponse ToResponse(AuthResult a)
        => new(a.Access.Token, a.Access.ExpiresAtUtc, a.RefreshToken, a.User.ToDto());

    private static string? Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();

    private static async Task SendSystemMessageAsync(IEnumerable<INotificationChannel> channels, ChannelType type,
        string destination, string title, string body, CancellationToken ct)
    {
        var channel = channels.FirstOrDefault(c => c.Channel == type);
        if (channel is null) return;
        try
        {
            await channel.SendAsync(new NotificationMessage
            {
                Channel = type, Destination = destination, Title = title, Body = body,
            }, ct);
        }
        catch { /* best-effort: el código queda persistido para reintento */ }
    }
}
