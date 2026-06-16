using System.Security.Claims;
using Wardkitten.Api.Security;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Services;
using Wardkitten.Domain.Teams;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Api.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/teams").WithTags("Teams").RequireAuthorization();

        g.MapGet("/", async (ClaimsPrincipal p, TeamService svc, IUserRepository users, IClock clock, CancellationToken ct) =>
        {
            var teams = await svc.ListForUserAsync(p.UserId()!, ct);
            var dtos = new List<TeamDto>();
            foreach (var team in teams) dtos.Add(await ToDtoAsync(team, users, clock, ct));
            return Results.Ok(dtos);
        });

        g.MapPost("/", async (CreateTeamRequest req, ClaimsPrincipal p, TeamService svc, IUserRepository users, IClock clock, CancellationToken ct) =>
        {
            var r = await svc.CreateAsync(p.UserId()!, req.Name, ct);
            return r.Success ? Results.Ok(await ToDtoAsync(r.Value!, users, clock, ct)) : Results.BadRequest(new { error = r.Error });
        });

        g.MapGet("/{id}", async (string id, ClaimsPrincipal p, TeamService svc, IUserRepository users, IClock clock, CancellationToken ct) =>
        {
            var team = await svc.GetAsync(id, p.UserId()!, ct);
            return team is null ? Results.NotFound() : Results.Ok(await ToDtoAsync(team, users, clock, ct));
        });

        g.MapDelete("/{id}", async (string id, ClaimsPrincipal p, TeamService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(id, p.UserId()!, ct)).Success ? Results.NoContent() : Results.NotFound());

        g.MapPost("/{id}/members", async (string id, AddMemberRequest req, ClaimsPrincipal p, TeamService svc, CancellationToken ct) =>
        {
            var r = await svc.AddMemberByEmailAsync(id, p.UserId()!, req.Email, ct);
            return r.Success ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
        });

        g.MapDelete("/{id}/members/{userId}", async (string id, string userId, ClaimsPrincipal p, TeamService svc, CancellationToken ct) =>
            (await svc.RemoveMemberAsync(id, p.UserId()!, userId, ct)).Success ? Results.NoContent() : Results.NotFound());

        g.MapPut("/{id}/oncall", async (string id, SetOnCallRequest req, ClaimsPrincipal p, TeamService svc, CancellationToken ct) =>
        {
            var r = await svc.SetOnCallAsync(id, p.UserId()!, req.AnchorUtc, req.ShiftSeconds, req.RotationUserIds, ct);
            return r.Success ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
        });

        g.MapPost("/{id}/oncall/overrides", async (string id, AddOnCallOverrideRequest req, ClaimsPrincipal p, TeamService svc, CancellationToken ct) =>
        {
            var r = await svc.AddOverrideAsync(id, p.UserId()!, req.StartUtc, req.EndUtc, req.UserId, ct);
            return r.Success ? Results.NoContent() : Results.BadRequest(new { error = r.Error });
        });
    }

    private static async Task<TeamDto> ToDtoAsync(Team team, IUserRepository users, IClock clock, CancellationToken ct)
    {
        var members = new List<TeamMemberDto>();
        async Task AddMember(string userId, bool isOwner)
        {
            var u = await users.GetByIdAsync(userId, ct);
            members.Add(new TeamMemberDto(userId, u?.Email ?? "?", u?.DisplayName ?? "?", isOwner));
        }
        await AddMember(team.OwnerId, true);
        foreach (var memberId in team.MemberUserIds) await AddMember(memberId, false);

        OnCallDto? onCall = team.OnCall is null ? null : new OnCallDto(
            team.OnCall.AnchorUtc, team.OnCall.ShiftSeconds, team.OnCall.RotationUserIds,
            team.OnCall.Overrides.Select(o => new OnCallOverrideDto(o.StartUtc, o.EndUtc, o.UserId)).ToList());

        return new TeamDto(team.Id, team.Name, team.OwnerId, members, onCall, team.OnCall?.CurrentOnCall(clock.UtcNow));
    }
}
