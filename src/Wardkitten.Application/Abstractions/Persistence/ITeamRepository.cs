using Wardkitten.Domain.Teams;

namespace Wardkitten.Application.Abstractions.Persistence;

public interface ITeamRepository : IRepository<Team>
{
    /// <summary>Equipos donde el usuario es owner o miembro.</summary>
    Task<IReadOnlyList<Team>> GetForUserAsync(string userId, CancellationToken ct = default);
}
