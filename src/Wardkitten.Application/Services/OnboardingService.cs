// Feature: F01.04 — asistente de bienvenida (onboarding)
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.Notifications;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

/// <summary>Lo que el asistente necesita saber para retomar al usuario donde lo dejó.</summary>
public sealed record OnboardingState(User User, long WatchCount, PlanLimits Limits)
{
    public bool Completed => User.OnboardingCompletedAtUtc is not null;
}

/// <summary>
/// Asistente de bienvenida para cuentas nuevas (F01.04): perfil (nombre, zona horaria, idioma), canales por
/// defecto con los que nacerán sus watches (F02.05) y cierre (terminado o saltado). El primer watch lo crea
/// el propio asistente con <see cref="WatchService"/>.
/// </summary>
public sealed class OnboardingService
{
    /// <summary>Idiomas de la interfaz. El primero es el de por defecto.</summary>
    public static readonly IReadOnlyList<string> SupportedLocales = new[] { "es", "en" };

    public const int MaxDisplayNameLength = 80;

    private readonly IUserRepository _users;
    private readonly IWatchRepository _watches;
    private readonly IClock _clock;

    public OnboardingService(IUserRepository users, IWatchRepository watches, IClock clock)
    {
        _users = users;
        _watches = watches;
        _clock = clock;
    }

    public async Task<Result<OnboardingState>> GetStateAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<OnboardingState>.Fail("Usuario no encontrado.");
        var count = await _watches.CountByUserAsync(userId, ct);
        return Result<OnboardingState>.Ok(new OnboardingState(user, count, PlanCatalog.For(user.Plan)));
    }

    public async Task<Result<User>> UpdateProfileAsync(string userId, string? displayName, string? timeZoneId,
        string? locale, CancellationToken ct = default)
    {
        var name = displayName?.Trim() ?? string.Empty;
        if (name.Length == 0) return Result<User>.Fail("Escribe tu nombre.");
        if (name.Length > MaxDisplayNameLength)
            return Result<User>.Fail($"El nombre no puede superar {MaxDisplayNameLength} caracteres.");

        var tz = timeZoneId?.Trim() ?? string.Empty;
        if (tz.Length == 0 || !TimeZoneInfo.TryFindSystemTimeZoneById(tz, out _))
            return Result<User>.Fail("Zona horaria no válida.");

        var lang = locale?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!SupportedLocales.Contains(lang)) return Result<User>.Fail("Idioma no soportado.");

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<User>.Fail("Usuario no encontrado.");

        user.DisplayName = name;
        user.TimeZoneId = tz;
        user.Locale = lang;
        await _users.ReplaceAsync(user, ct);
        return Result<User>.Ok(user);
    }

    /// <summary>
    /// Guarda los canales por defecto (F02.05). Cada uno debe poder entregar ya (ver
    /// <see cref="ChannelBindingRules"/>); se descartan duplicados exactos y se renumera el orden. Una lista
    /// vacía vuelve al comportamiento de siempre: los watches nuevos avisan solo por Email.
    /// </summary>
    public async Task<Result<User>> SetDefaultChannelsAsync(string userId, IReadOnlyList<ChannelBinding>? bindings,
        CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<User>.Fail("Usuario no encontrado.");

        var accepted = new List<ChannelBinding>();
        foreach (var binding in bindings ?? Array.Empty<ChannelBinding>())
        {
            var destination = string.IsNullOrWhiteSpace(binding.DestinationOverride) ? null : binding.DestinationOverride.Trim();
            var candidate = new ChannelBinding
            {
                ChannelType = binding.ChannelType,
                Enabled = true,
                DestinationOverride = destination,
                Order = accepted.Count,
                EscalationDelaySeconds = Math.Max(0, binding.EscalationDelaySeconds),
                QuietHours = binding.QuietHours,
            };

            var valid = ChannelBindingRules.Validate(candidate, user);
            if (!valid.Success) return Result<User>.Fail(valid.Error!);

            if (accepted.Any(a => a.ChannelType == candidate.ChannelType
                               && string.Equals(a.DestinationOverride, candidate.DestinationOverride, StringComparison.OrdinalIgnoreCase)))
                continue;

            accepted.Add(candidate);
        }

        user.DefaultChannelBindings = accepted;
        await _users.ReplaceAsync(user, ct);
        return Result<User>.Ok(user);
    }

    /// <summary>El usuario terminó el asistente.</summary>
    public Task<Result> CompleteAsync(string userId, CancellationToken ct = default) => CloseAsync(userId, ct);

    /// <summary>
    /// El usuario saltó el asistente (o descartó el aviso del panel). Hoy tiene el mismo efecto que terminarlo;
    /// se mantiene separado para poder distinguirlos en analítica.
    /// </summary>
    public Task<Result> SkipAsync(string userId, CancellationToken ct = default) => CloseAsync(userId, ct);

    private async Task<Result> CloseAsync(string userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Fail("Usuario no encontrado.");
        if (user.OnboardingCompletedAtUtc is not null) return Result.Ok();   // idempotente: conserva la primera fecha

        user.OnboardingCompletedAtUtc = _clock.UtcNow;
        await _users.ReplaceAsync(user, ct);
        return Result.Ok();
    }
}
