using Es.Nimita.Infra.Mongo;
using Es.Nimita.Infra.Mongo.Leasing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Wardkitten.Application.Abstractions;
using Wardkitten.Application.Abstractions.Persistence;
using Wardkitten.Infrastructure.Mongo;
using Wardkitten.Infrastructure.Mongo.Repositories;
using Wardkitten.Infrastructure.Time;

namespace Wardkitten.Infrastructure.DependencyInjection;

public static class InfrastructureRegistration
{
    /// <summary>
    /// Base de datos por defecto cuando la configuración no la indica. El <c>MongoSettings</c> de
    /// Es.Nimita.Infra.Mongo defaultea a cadena vacía; Wardkitten conserva su default histórico
    /// (es el nombre de la BBDD de producción). Lo protege MongoSettingsRegistrationTests.
    /// </summary>
    private const string DefaultDatabaseName = "Wardkitten";

    /// <summary>
    /// Registra MongoDB (cliente, contexto, repositorios) y servicios base de infraestructura.
    /// Conexión por <c>MONGOSETTINGS_CONNECTION</c> / <c>MONGOSETTINGS_DATABASENAME</c> (ver SECURITY.md).
    /// </summary>
    public static IServiceCollection AddWardkittenInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MongoDbConfigurator.Configure(); // SIEMPRE antes de construir el cliente/contexto

        var defaults = new MongoSettings();
        var databaseName = config["MONGOSETTINGS_DATABASENAME"];
        var settings = new MongoSettings
        {
            Connection = config["MONGOSETTINGS_CONNECTION"] ?? config["ConnectionStrings:Mongo"] ?? defaults.Connection,
            DatabaseName = string.IsNullOrWhiteSpace(databaseName) ? DefaultDatabaseName : databaseName,
        };

        services.AddSingleton(Options.Create(settings));
        services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.Connection));
        services.AddSingleton<MongoContext>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IWatchRepository, WatchRepository>();
        services.AddSingleton<ICheckInRepository, CheckInRepository>();
        services.AddSingleton<IPingProbeRepository, PingProbeRepository>();
        services.AddSingleton<IIncidentRepository, IncidentRepository>();
        services.AddSingleton<IEscalationPolicyRepository, EscalationPolicyRepository>();
        services.AddSingleton<ISubscriptionRepository, SubscriptionRepository>();
        services.AddSingleton<IWalletRepository, WalletRepository>();
        services.AddSingleton<ICreditTransactionRepository, CreditTransactionRepository>();
        services.AddSingleton<IChannelRateRepository, ChannelRateRepository>();
        services.AddSingleton<INotificationLogRepository, NotificationLogRepository>();
        services.AddSingleton<IStatusPageRepository, StatusPageRepository>();
        services.AddSingleton<ITeamRepository, TeamRepository>();
        // Lease del paquete Es.Nimita.Infra.Mongo, sobre la MISMA colección de siempre
        // (MongoContext.Leases -> "leases") y con el reloj de Wardkitten adaptado a TimeProvider.
        services.AddSingleton<ILeaseStore>(sp => new MongoLeaseStore(
            sp.GetRequiredService<MongoContext>().Leases,
            new ClockTimeProvider(sp.GetRequiredService<IClock>())));

        return services;
    }

    /// <summary>Crea colecciones especiales (time-series) e índices. Llamar en el arranque.</summary>
    public static async Task InitializeWardkittenInfrastructureAsync(this IServiceProvider provider, CancellationToken ct = default)
    {
        var ctx = provider.GetRequiredService<MongoContext>();
        await ctx.InitializeAsync(ct);
    }
}
