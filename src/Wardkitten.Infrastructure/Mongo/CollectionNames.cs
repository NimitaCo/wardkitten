namespace Wardkitten.Infrastructure.Mongo;

/// <summary>Nombres de colección centralizados (camelCase, coherente con la convención de elementos).</summary>
public static class CollectionNames
{
    public const string Users = "users";
    public const string RefreshTokens = "refreshTokens";
    public const string Watches = "watches";
    public const string CheckIns = "checkIns";
    public const string Incidents = "incidents";
    public const string EscalationPolicies = "escalationPolicies";
    public const string Subscriptions = "subscriptions";
    public const string Wallets = "wallets";
    public const string CreditTransactions = "creditTransactions";
    public const string ChannelRates = "channelRates";
    public const string NotificationLogs = "notificationLogs";
    public const string StatusPages = "statusPages";
    public const string Teams = "teams";
    public const string Leases = "leases";
}
