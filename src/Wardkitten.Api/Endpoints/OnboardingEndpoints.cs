// Feature: F01.04 — asistente de bienvenida (onboarding) · F02.05 canales por defecto · F05.06 prueba de canal
using System.Security.Claims;
using Wardkitten.Api.Mapping;
using Wardkitten.Api.Security;
using Wardkitten.Application.Services;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Api.Endpoints;

public static class OnboardingEndpoints
{
    public static void MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/onboarding").WithTags("Onboarding").RequireAuthorization();

        g.MapGet("/state", async (ClaimsPrincipal p, OnboardingService svc, TelegramLinkService telegram, CancellationToken ct) =>
        {
            var r = await svc.GetStateAsync(p.UserId()!, ct);
            return r.Success ? Results.Ok(r.Value!.ToDto(telegram.IsConfigured)) : Results.NotFound(new { error = r.Error });
        });

        g.MapPut("/profile", async (UpdateProfileRequest req, ClaimsPrincipal p, OnboardingService svc, CancellationToken ct) =>
        {
            var r = await svc.UpdateProfileAsync(p.UserId()!, req.DisplayName, req.TimeZoneId, req.Locale, ct);
            return r.Success ? Results.Ok(r.Value!.ToDto()) : Results.BadRequest(new { error = r.Error });
        });

        g.MapPut("/channels", async (UpdateChannelsRequest req, ClaimsPrincipal p, OnboardingService svc, CancellationToken ct) =>
        {
            var r = await svc.SetDefaultChannelsAsync(p.UserId()!, req.Bindings, ct);
            return r.Success ? Results.Ok(r.Value!.DefaultChannelBindings) : Results.BadRequest(new { error = r.Error });
        });

        // «Enviar prueba»: siempre 200 con el resultado, para que la UI enseñe el mensaje tal cual. Limitado
        // por IP como el resto de envíos a demanda (evita usarlo para spamear un email o una URL).
        g.MapPost("/channels/test", async (ChannelTestRequest req, ClaimsPrincipal p, ChannelTestService svc, CancellationToken ct) =>
        {
            var r = await svc.SendTestAsync(p.UserId()!, req.Binding, ct);
            return Results.Ok(new ChannelTestResultDto(r.Success, r.Success ? r.Value! : r.Error!));
        }).RequireRateLimiting("auth");

        g.MapPost("/complete", async (ClaimsPrincipal p, OnboardingService svc, CancellationToken ct) =>
            (await svc.CompleteAsync(p.UserId()!, ct)).Success ? Results.NoContent() : Results.NotFound());

        g.MapPost("/skip", async (ClaimsPrincipal p, OnboardingService svc, CancellationToken ct) =>
            (await svc.SkipAsync(p.UserId()!, ct)).Success ? Results.NoContent() : Results.NotFound());
    }
}
