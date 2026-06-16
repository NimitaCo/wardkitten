using Wardkitten.Domain.Common;

namespace Wardkitten.Domain.Teams;

/// <summary>
/// Equipo de usuarios con guardias (on-call). Permite escalar incidentes no reconocidos a la persona de
/// guardia. Disponible en el plan Team. Feature: F12.01.
/// </summary>
public sealed class Team : Entity
{
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Miembros del equipo (ids de usuario). El owner también es miembro implícito.</summary>
    public List<string> MemberUserIds { get; set; } = new();

    public OnCallSchedule? OnCall { get; set; }

    public bool IsMember(string userId) => OwnerId == userId || MemberUserIds.Contains(userId);
}

/// <summary>
/// Calendario de guardias: rotación por turnos de duración fija desde un instante ancla, con posibles
/// overrides manuales para ventanas concretas. Feature: F12.02.
/// </summary>
public sealed class OnCallSchedule
{
    /// <summary>Inicio de la rotación (UTC).</summary>
    public DateTime AnchorUtc { get; set; }

    /// <summary>Duración de cada turno en segundos (por defecto, semanal).</summary>
    public int ShiftSeconds { get; set; } = 7 * 24 * 3600;

    /// <summary>Usuarios en la rotación, en orden de turno.</summary>
    public List<string> RotationUserIds { get; set; } = new();

    /// <summary>Overrides manuales (vacaciones, cambios puntuales) que tienen prioridad.</summary>
    public List<OnCallOverride> Overrides { get; set; } = new();

    /// <summary>Usuario de guardia ahora mismo, o null si no hay rotación configurada.</summary>
    public string? CurrentOnCall(DateTime nowUtc)
    {
        var ovr = Overrides.FirstOrDefault(o => o.Contains(nowUtc));
        if (ovr is not null) return ovr.UserId;

        if (RotationUserIds.Count == 0 || ShiftSeconds <= 0) return null;
        var elapsed = nowUtc - AnchorUtc;
        if (elapsed < TimeSpan.Zero) return RotationUserIds[0];

        var shiftIndex = (long)(elapsed.TotalSeconds / ShiftSeconds) % RotationUserIds.Count;
        return RotationUserIds[(int)shiftIndex];
    }
}

public sealed class OnCallOverride
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string UserId { get; set; } = string.Empty;

    public bool Contains(DateTime utc) => utc >= StartUtc && utc <= EndUtc;
}
