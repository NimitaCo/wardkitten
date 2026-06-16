using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Teams;

namespace Wardkitten.Application.Services;

/// <summary>
/// Gestión de equipos y guardias (on-call). Solo el owner puede modificar; los miembros pueden ver.
/// Requiere plan Team. Feature: F12.
/// </summary>
public sealed class TeamService
{
    private readonly ITeamRepository _teams;
    private readonly IUserRepository _users;

    public TeamService(ITeamRepository teams, IUserRepository users)
    {
        _teams = teams;
        _users = users;
    }

    public Task<IReadOnlyList<Team>> ListForUserAsync(string userId, CancellationToken ct = default)
        => _teams.GetForUserAsync(userId, ct);

    public async Task<Team?> GetAsync(string teamId, string userId, CancellationToken ct = default)
    {
        var team = await _teams.GetByIdAsync(teamId, ct);
        return team is not null && team.IsMember(userId) ? team : null;
    }

    public async Task<Result<Team>> CreateAsync(string ownerId, string name, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(ownerId, ct);
        if (user is null) return Result<Team>.Fail("Usuario no encontrado.");
        if (!PlanCatalog.For(user.Plan).TeamFeatures)
            return Result<Team>.Fail("Los equipos requieren el plan Team.");
        if (string.IsNullOrWhiteSpace(name)) return Result<Team>.Fail("El nombre es obligatorio.");

        var team = new Team { OwnerId = ownerId, Name = name.Trim() };
        await _teams.InsertAsync(team, ct);
        return Result<Team>.Ok(team);
    }

    public async Task<Result> AddMemberByEmailAsync(string teamId, string ownerId, string email, CancellationToken ct = default)
    {
        var team = await OwnedTeamAsync(teamId, ownerId, ct);
        if (team is null) return Result.Fail("Equipo no encontrado.");
        var member = await _users.GetByEmailAsync(email, ct);
        if (member is null) return Result.Fail("No hay ningún usuario con ese email.");
        if (member.Id != ownerId && !team.MemberUserIds.Contains(member.Id))
        {
            team.MemberUserIds.Add(member.Id);
            await _teams.ReplaceAsync(team, ct);
        }
        return Result.Ok();
    }

    public async Task<Result> RemoveMemberAsync(string teamId, string ownerId, string memberUserId, CancellationToken ct = default)
    {
        var team = await OwnedTeamAsync(teamId, ownerId, ct);
        if (team is null) return Result.Fail("Equipo no encontrado.");
        team.MemberUserIds.Remove(memberUserId);
        team.OnCall?.RotationUserIds.Remove(memberUserId);
        await _teams.ReplaceAsync(team, ct);
        return Result.Ok();
    }

    public async Task<Result> SetOnCallAsync(string teamId, string ownerId, DateTime anchorUtc, int shiftSeconds,
        List<string> rotationUserIds, CancellationToken ct = default)
    {
        var team = await OwnedTeamAsync(teamId, ownerId, ct);
        if (team is null) return Result.Fail("Equipo no encontrado.");
        if (shiftSeconds <= 0) return Result.Fail("La duración del turno debe ser positiva.");

        // Solo miembros válidos en la rotación.
        var valid = rotationUserIds.Where(team.IsMember).Distinct().ToList();
        team.OnCall = new OnCallSchedule
        {
            AnchorUtc = anchorUtc,
            ShiftSeconds = shiftSeconds,
            RotationUserIds = valid,
            Overrides = team.OnCall?.Overrides ?? new(),
        };
        await _teams.ReplaceAsync(team, ct);
        return Result.Ok();
    }

    public async Task<Result> AddOverrideAsync(string teamId, string ownerId, DateTime startUtc, DateTime endUtc, string userId, CancellationToken ct = default)
    {
        var team = await OwnedTeamAsync(teamId, ownerId, ct);
        if (team is null) return Result.Fail("Equipo no encontrado.");
        if (endUtc <= startUtc) return Result.Fail("La ventana del override no es válida.");
        team.OnCall ??= new OnCallSchedule { AnchorUtc = startUtc };
        team.OnCall.Overrides.Add(new OnCallOverride { StartUtc = startUtc, EndUtc = endUtc, UserId = userId });
        await _teams.ReplaceAsync(team, ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(string teamId, string ownerId, CancellationToken ct = default)
    {
        var team = await OwnedTeamAsync(teamId, ownerId, ct);
        if (team is null) return Result.Fail("Equipo no encontrado.");
        await _teams.DeleteAsync(teamId, ct);
        return Result.Ok();
    }

    public async Task<string?> GetCurrentOnCallAsync(string teamId, DateTime nowUtc, CancellationToken ct = default)
    {
        var team = await _teams.GetByIdAsync(teamId, ct);
        return team?.OnCall?.CurrentOnCall(nowUtc);
    }

    private async Task<Team?> OwnedTeamAsync(string teamId, string ownerId, CancellationToken ct)
    {
        var team = await _teams.GetByIdAsync(teamId, ct);
        return team is not null && team.OwnerId == ownerId ? team : null;
    }
}
