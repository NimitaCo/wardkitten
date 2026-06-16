using System.Security.Claims;
using Wardkitten.Api.Mapping;
using Wardkitten.Api.Security;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Services;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Api.Endpoints;

public static class WatchEndpoints
{
    public static void MapWatchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/watches").WithTags("Watches").RequireAuthorization();

        g.MapGet("/", async (ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
        {
            var watches = await svc.ListByUserAsync(p.UserId()!, ct);
            return Results.Ok(watches.Select(w => w.ToDto()));
        });

        g.MapGet("/{id}", async (string id, ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
        {
            var watch = await svc.GetAsync(id, p.UserId()!, ct);
            return watch is null ? Results.NotFound() : Results.Ok(watch.ToDto());
        });

        g.MapPost("/", async (WatchRequest req, ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
        {
            var r = await svc.CreateAsync(p.UserId()!, ToInput(req), ct);
            return r.Success ? Results.Created($"/api/watches/{r.Value!.Id}", r.Value.ToDto())
                             : Results.BadRequest(new { error = r.Error });
        });

        g.MapPut("/{id}", async (string id, WatchRequest req, ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
        {
            var r = await svc.UpdateAsync(id, p.UserId()!, ToInput(req), ct);
            return r.Success ? Results.Ok(r.Value!.ToDto()) : Results.BadRequest(new { error = r.Error });
        });

        g.MapDelete("/{id}", async (string id, ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
        {
            var r = await svc.DeleteAsync(id, p.UserId()!, ct);
            return r.Success ? Results.NoContent() : Results.NotFound(new { error = r.Error });
        });

        g.MapPost("/{id}/pause", async (string id, ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
            (await svc.PauseAsync(id, p.UserId()!, ct)).Success ? Results.NoContent() : Results.NotFound());

        g.MapPost("/{id}/resume", async (string id, ClaimsPrincipal p, WatchService svc, CancellationToken ct) =>
            (await svc.ResumeAsync(id, p.UserId()!, ct)).Success ? Results.NoContent() : Results.NotFound());

        g.MapPost("/{id}/checkin", async (string id, ClaimsPrincipal p, CheckInService svc, CancellationToken ct) =>
        {
            var r = await svc.RecordManualAsync(id, p.UserId()!, CheckInSource.App, ct);
            return r.Success ? Results.NoContent() : Results.NotFound(new { error = r.Error });
        });

        g.MapGet("/{id}/checkins", async (string id, ClaimsPrincipal p, WatchService watches, ICheckInRepository checkIns, CancellationToken ct) =>
        {
            var watch = await watches.GetAsync(id, p.UserId()!, ct);
            if (watch is null) return Results.NotFound();
            var recent = await checkIns.GetRecentByWatchAsync(id, 50, ct);
            return Results.Ok(recent.Select(c => c.ToDto()));
        });
    }

    private static WatchInput ToInput(WatchRequest req)
        => new(req.Name, req.Description, req.Type, req.Schedule, req.Tolerance, req.ChannelBindings,
               req.Severity, req.Tags, req.ProjectId, req.EscalationTeamId, req.TeamEscalationDelaySeconds);
}
