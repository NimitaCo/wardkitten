using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Wardkitten.Domain.Billing;
using Wardkitten.Domain.CheckIns;
using Wardkitten.Domain.Identity;
using Wardkitten.Domain.Incidents;
using Wardkitten.Domain.Leasing;
using Wardkitten.Domain.Notifications;
using Wardkitten.Domain.StatusPages;
using Wardkitten.Domain.Teams;
using Wardkitten.Domain.Watches;

namespace Wardkitten.Infrastructure.Mongo;

/// <summary>
/// Acceso tipado a la base de datos. Centraliza las colecciones e <see cref="InitializeAsync"/> crea la
/// colección time-series de check-ins y los índices (idempotente; seguro de llamar en cada arranque).
/// </summary>
public sealed class MongoContext
{
    public IMongoDatabase Database { get; }

    public MongoContext(IMongoClient client, IOptions<MongoSettings> options)
    {
        Database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<User> Users => Database.GetCollection<User>(CollectionNames.Users);
    public IMongoCollection<RefreshToken> RefreshTokens => Database.GetCollection<RefreshToken>(CollectionNames.RefreshTokens);
    public IMongoCollection<Watch> Watches => Database.GetCollection<Watch>(CollectionNames.Watches);
    public IMongoCollection<CheckIn> CheckIns => Database.GetCollection<CheckIn>(CollectionNames.CheckIns);
    public IMongoCollection<Incident> Incidents => Database.GetCollection<Incident>(CollectionNames.Incidents);
    public IMongoCollection<EscalationPolicy> EscalationPolicies => Database.GetCollection<EscalationPolicy>(CollectionNames.EscalationPolicies);
    public IMongoCollection<Subscription> Subscriptions => Database.GetCollection<Subscription>(CollectionNames.Subscriptions);
    public IMongoCollection<Wallet> Wallets => Database.GetCollection<Wallet>(CollectionNames.Wallets);
    public IMongoCollection<CreditTransaction> CreditTransactions => Database.GetCollection<CreditTransaction>(CollectionNames.CreditTransactions);
    public IMongoCollection<ChannelRate> ChannelRates => Database.GetCollection<ChannelRate>(CollectionNames.ChannelRates);
    public IMongoCollection<NotificationLog> NotificationLogs => Database.GetCollection<NotificationLog>(CollectionNames.NotificationLogs);
    public IMongoCollection<StatusPage> StatusPages => Database.GetCollection<StatusPage>(CollectionNames.StatusPages);
    public IMongoCollection<Team> Teams => Database.GetCollection<Team>(CollectionNames.Teams);
    public IMongoCollection<Lease> Leases => Database.GetCollection<Lease>(CollectionNames.Leases);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await EnsureCheckInsTimeSeriesAsync(ct);
        await EnsureIndexesAsync(ct);
    }

    private async Task EnsureCheckInsTimeSeriesAsync(CancellationToken ct)
    {
        var names = await (await Database.ListCollectionNamesAsync(cancellationToken: ct)).ToListAsync(ct);
        if (names.Contains(CollectionNames.CheckIns)) return;

        await Database.CreateCollectionAsync(
            CollectionNames.CheckIns,
            new CreateCollectionOptions
            {
                TimeSeriesOptions = new TimeSeriesOptions(
                    timeField: "receivedAtUtc",
                    metaField: "watchId",
                    granularity: TimeSeriesGranularity.Minutes),
            },
            ct);
    }

    private async Task EnsureIndexesAsync(CancellationToken ct)
    {
        await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" }), cancellationToken: ct);

        await RefreshTokens.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Ascending(t => t.TokenHash),
                new CreateIndexOptions { Name = "ix_refresh_hash" }),
            new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Ascending(t => t.UserId),
                new CreateIndexOptions { Name = "ix_refresh_user" }),
        }, ct);

        await Watches.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Watch>(Builders<Watch>.IndexKeys.Ascending(w => w.UserId),
                new CreateIndexOptions { Name = "ix_watches_user" }),
            new CreateIndexModel<Watch>(Builders<Watch>.IndexKeys.Ascending(w => w.NextDueAtUtc),
                new CreateIndexOptions { Name = "ix_watches_due" }),
            // pingToken único solo para los watches de tipo Ping (los manuales lo dejan vacío).
            new CreateIndexModel<Watch>(Builders<Watch>.IndexKeys.Ascending(w => w.PingToken),
                new CreateIndexOptions<Watch>
                {
                    Name = "ux_watches_pingtoken",
                    Unique = true,
                    PartialFilterExpression = Builders<Watch>.Filter.Gt(w => w.PingToken, ""),
                }),
        }, ct);

        await Incidents.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Incident>(Builders<Incident>.IndexKeys.Ascending(i => i.UserId).Descending(i => i.OpenedAtUtc),
                new CreateIndexOptions { Name = "ix_incidents_user" }),
            // Un único incidente ABIERTO por watch (garantía de idempotencia a nivel de BBDD).
            new CreateIndexModel<Incident>(Builders<Incident>.IndexKeys.Ascending(i => i.WatchId),
                new CreateIndexOptions<Incident>
                {
                    Name = "ux_incident_open_per_watch",
                    Unique = true,
                    PartialFilterExpression = Builders<Incident>.Filter.Eq(i => i.State, IncidentState.Open),
                }),
        }, ct);

        await Subscriptions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Subscription>(Builders<Subscription>.IndexKeys.Ascending(s => s.UserId),
                new CreateIndexOptions { Unique = true, Name = "ux_sub_user" }),
            new CreateIndexModel<Subscription>(Builders<Subscription>.IndexKeys.Ascending(s => s.StripeSubscriptionId),
                new CreateIndexOptions { Name = "ix_sub_stripe" }),
        }, ct);

        await Wallets.Indexes.CreateOneAsync(new CreateIndexModel<Wallet>(
            Builders<Wallet>.IndexKeys.Ascending(w => w.UserId),
            new CreateIndexOptions { Unique = true, Name = "ux_wallet_user" }), cancellationToken: ct);

        await CreditTransactions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<CreditTransaction>(Builders<CreditTransaction>.IndexKeys.Ascending(t => t.UserId).Descending(t => t.CreatedAtUtc),
                new CreateIndexOptions { Name = "ix_credit_user" }),
            new CreateIndexModel<CreditTransaction>(Builders<CreditTransaction>.IndexKeys.Ascending(t => t.IdempotencyKey),
                new CreateIndexOptions { Unique = true, Sparse = true, Name = "ux_credit_idem" }),
        }, ct);

        await EscalationPolicies.Indexes.CreateOneAsync(new CreateIndexModel<EscalationPolicy>(
            Builders<EscalationPolicy>.IndexKeys.Ascending(p => p.UserId),
            new CreateIndexOptions { Name = "ix_escpolicy_user" }), cancellationToken: ct);

        await NotificationLogs.Indexes.CreateOneAsync(new CreateIndexModel<NotificationLog>(
            Builders<NotificationLog>.IndexKeys.Ascending(n => n.UserId).Descending(n => n.SentAtUtc),
            new CreateIndexOptions { Name = "ix_notiflog_user" }), cancellationToken: ct);

        await StatusPages.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<StatusPage>(Builders<StatusPage>.IndexKeys.Ascending(s => s.Slug),
                new CreateIndexOptions { Unique = true, Name = "ux_statuspage_slug" }),
            new CreateIndexModel<StatusPage>(Builders<StatusPage>.IndexKeys.Ascending(s => s.UserId),
                new CreateIndexOptions { Name = "ix_statuspage_user" }),
        }, ct);

        await Teams.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Team>(Builders<Team>.IndexKeys.Ascending(t => t.OwnerId),
                new CreateIndexOptions { Name = "ix_team_owner" }),
            new CreateIndexModel<Team>(Builders<Team>.IndexKeys.Ascending(t => t.MemberUserIds),
                new CreateIndexOptions { Name = "ix_team_members" }),
        }, ct);

        await Leases.Indexes.CreateOneAsync(new CreateIndexModel<Lease>(
            Builders<Lease>.IndexKeys.Ascending(l => l.ExpiresAtUtc),
            new CreateIndexOptions { Name = "ix_lease_expiry" }), cancellationToken: ct);
    }
}
