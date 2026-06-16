using Wardkitten.Domain.Billing;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Watches;
using Wardkitten.Shared.Contracts;

namespace Wardkitten.Api.Mapping;

public static class DtoMappings
{
    public static UserDto ToDto(this User u) => new(
        u.Id, u.Email, u.DisplayName, u.TimeZoneId, u.Locale, u.Plan.ToString(),
        u.EmailVerified, u.PhoneVerified, u.Phone, u.Roles);

    public static WatchDto ToDto(this Watch w) => new(
        w.Id, w.Name, w.Description, w.Type, w.Schedule, w.Tolerance, w.ChannelBindings,
        w.Severity, w.Status, w.Paused, w.NextDueAtUtc, w.LastCheckInAtUtc, w.ConsecutiveMisses,
        w.Type == WatchType.Ping ? w.PingToken : null, w.Tags, w.ProjectId, w.CurrentIncidentId,
        w.CurrentStreak, w.BestStreak, w.EscalationTeamId, w.TeamEscalationDelaySeconds, w.CreatedAtUtc);

    public static CheckInDto ToDto(this CheckIn c) => new(c.Id, c.Kind.ToString(), c.Source.ToString(), c.ReceivedAtUtc, c.DurationMs);

    public static WalletDto ToDto(this Wallet w) => new(w.BalanceCredits, w.MinThresholdCredits, w.Currency, w.IsBelowThreshold);

    public static CreditTransactionDto ToDto(this CreditTransaction t) => new(
        t.Id, t.Type.ToString(), t.AmountCredits, t.BalanceAfter, t.Reason, t.Channel?.ToString(), t.CreatedAtUtc);

    public static IncidentDto ToDto(this Incident i) => new(
        i.Id, i.WatchId, i.WatchName, i.Severity.ToString(), i.State.ToString(),
        i.OpenedAtUtc, i.AcknowledgedAtUtc, i.ResolvedAtUtc, i.CurrentEscalationStep);
}
