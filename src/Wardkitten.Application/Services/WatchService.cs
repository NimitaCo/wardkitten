using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Application.Common;
using Wardkitten.Application.RealTime;
using Wardkitten.Application.Security;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Application.Services;

public sealed record WatchInput(
    string Name,
    string? Description,
    WatchType Type,
    Schedule Schedule,
    Tolerance Tolerance,
    List<ChannelBinding> ChannelBindings,
    Severity Severity,
    List<string>? Tags,
    string? ProjectId);

/// <summary>
/// Alta/edición/borrado de watches con validación de schedule y aplicación de los límites del plan
/// (siempre en servidor). Genera el pingToken inadivinable para watches de tipo Ping. Feature: F02.
/// </summary>
public sealed class WatchService
{
    private readonly IWatchRepository _watches;
    private readonly IUserRepository _users;
    private readonly IWatchEventPublisher _events;
    private readonly IClock _clock;

    public WatchService(IWatchRepository watches, IUserRepository users, IWatchEventPublisher events, IClock clock)
    {
        _watches = watches;
        _users = users;
        _events = events;
        _clock = clock;
    }

    public Task<IReadOnlyList<Watch>> ListByUserAsync(string userId, CancellationToken ct = default)
        => _watches.GetByUserAsync(userId, ct);

    public async Task<Watch?> GetAsync(string watchId, string userId, CancellationToken ct = default)
    {
        var watch = await _watches.GetByIdAsync(watchId, ct);
        return watch is not null && watch.UserId == userId ? watch : null;
    }

    public async Task<Result<Watch>> CreateAsync(string userId, WatchInput input, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<Watch>.Fail("Usuario no encontrado.");

        var limits = PlanCatalog.For(user.Plan);
        if (await _watches.CountByUserAsync(userId, ct) >= limits.MaxWatches)
            return Result<Watch>.Fail($"Has alcanzado el límite de {limits.MaxWatches} watches de tu plan.");

        var validation = ValidateSchedule(input.Schedule, limits);
        if (!validation.Success) return Result<Watch>.Fail(validation.Error!);

        var schedule = input.Schedule;
        if (string.IsNullOrWhiteSpace(schedule.TimeZoneId) || schedule.TimeZoneId == "UTC")
            schedule.TimeZoneId = user.TimeZoneId;

        var watch = new Watch
        {
            UserId = userId,
            Name = input.Name.Trim(),
            Description = input.Description,
            Type = input.Type,
            Schedule = schedule,
            Tolerance = input.Tolerance,
            ChannelBindings = input.ChannelBindings.Count > 0 ? input.ChannelBindings : DefaultBindings(),
            Severity = input.Severity,
            Tags = input.Tags ?? new List<string>(),
            ProjectId = input.ProjectId,
            Status = WatchStatus.New,
            PingToken = input.Type == WatchType.Ping ? SecureTokenGenerator.New() : string.Empty,
        };
        watch.ScheduleNextFrom(_clock.UtcNow);

        await _watches.InsertAsync(watch, ct);
        await _events.WatchUpdatedAsync(watch, ct);
        return Result<Watch>.Ok(watch);
    }

    public async Task<Result<Watch>> UpdateAsync(string watchId, string userId, WatchInput input, CancellationToken ct = default)
    {
        var watch = await GetAsync(watchId, userId, ct);
        if (watch is null) return Result<Watch>.Fail("Watch no encontrado.");

        var user = await _users.GetByIdAsync(userId, ct);
        var limits = PlanCatalog.For(user!.Plan);
        var validation = ValidateSchedule(input.Schedule, limits);
        if (!validation.Success) return Result<Watch>.Fail(validation.Error!);

        var scheduleChanged = watch.Schedule.Kind != input.Schedule.Kind
            || watch.Schedule.IntervalSeconds != input.Schedule.IntervalSeconds
            || watch.Schedule.CronExpression != input.Schedule.CronExpression;

        watch.Name = input.Name.Trim();
        watch.Description = input.Description;
        watch.Type = input.Type;
        watch.Schedule = input.Schedule;
        if (string.IsNullOrWhiteSpace(watch.Schedule.TimeZoneId)) watch.Schedule.TimeZoneId = user.TimeZoneId;
        watch.Tolerance = input.Tolerance;
        watch.ChannelBindings = input.ChannelBindings;
        watch.Severity = input.Severity;
        watch.Tags = input.Tags ?? new List<string>();
        watch.ProjectId = input.ProjectId;
        if (watch.Type == WatchType.Ping && string.IsNullOrEmpty(watch.PingToken))
            watch.PingToken = SecureTokenGenerator.New();

        if (scheduleChanged) watch.ScheduleNextFrom(_clock.UtcNow);

        await _watches.ReplaceAsync(watch, ct);
        await _events.WatchUpdatedAsync(watch, ct);
        return Result<Watch>.Ok(watch);
    }

    public async Task<Result> DeleteAsync(string watchId, string userId, CancellationToken ct = default)
    {
        var watch = await GetAsync(watchId, userId, ct);
        if (watch is null) return Result.Fail("Watch no encontrado.");
        await _watches.DeleteAsync(watchId, ct);
        return Result.Ok();
    }

    public async Task<Result> PauseAsync(string watchId, string userId, CancellationToken ct = default)
    {
        var watch = await GetAsync(watchId, userId, ct);
        if (watch is null) return Result.Fail("Watch no encontrado.");
        watch.Pause();
        await _watches.ReplaceAsync(watch, ct);
        await _events.WatchUpdatedAsync(watch, ct);
        return Result.Ok();
    }

    public async Task<Result> ResumeAsync(string watchId, string userId, CancellationToken ct = default)
    {
        var watch = await GetAsync(watchId, userId, ct);
        if (watch is null) return Result.Fail("Watch no encontrado.");
        watch.Resume(_clock.UtcNow);
        await _watches.ReplaceAsync(watch, ct);
        await _events.WatchUpdatedAsync(watch, ct);
        return Result.Ok();
    }

    private static Result ValidateSchedule(Schedule schedule, PlanLimits limits)
    {
        if (!schedule.IsValid(out var error))
            return Result.Fail(error!);
        if (schedule.Kind == ScheduleKind.Interval && schedule.IntervalSeconds < limits.MinIntervalSeconds)
            return Result.Fail($"El intervalo mínimo de tu plan es {limits.MinIntervalSeconds} segundos.");
        return Result.Ok();
    }

    private static List<ChannelBinding> DefaultBindings()
        => new() { new ChannelBinding { ChannelType = ChannelType.Email, Enabled = true, Order = 0 } };
}
