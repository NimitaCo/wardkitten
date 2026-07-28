using Es.Nimita.Infra.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Wardkitten.Infrastructure.DependencyInjection;

namespace Wardkitten.Tests.Infrastructure;

/// <summary>
/// La base de datos por defecto es "Wardkitten" cuando no hay configuración: es el nombre que usa
/// producción, así que el default NO puede cambiar (p. ej. al adoptar un MongoSettings compartido
/// cuyo default sea cadena vacía).
/// </summary>
public class MongoSettingsRegistrationTests
{
    [Fact]
    public void WithoutConfiguration_DatabaseNameDefaultsToWardkitten()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddWardkittenInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<MongoSettings>>().Value;

        settings.DatabaseName.ShouldBe("Wardkitten");
        settings.Connection.ShouldBe("mongodb://localhost:27017");
    }

    [Fact]
    public void WithConfiguration_UsesConfiguredValues()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MONGOSETTINGS_CONNECTION"] = "mongodb://mongo.example:27017",
            ["MONGOSETTINGS_DATABASENAME"] = "WardkittenTest",
        }).Build();
        var services = new ServiceCollection();
        services.AddWardkittenInfrastructure(config);

        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<MongoSettings>>().Value;

        settings.Connection.ShouldBe("mongodb://mongo.example:27017");
        settings.DatabaseName.ShouldBe("WardkittenTest");
    }
}
