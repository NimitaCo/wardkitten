using Es.Nimita.Infra.Mongo;

namespace Wardkitten.Infrastructure.Mongo;

/// <summary>
/// Registra las convenciones BSON globales de Wardkitten. <b>Debe llamarse una sola vez y ANTES</b> de
/// construir cualquier <c>IMongoClient</c>/contexto o de serializar entidades (ver AGENTS.md).
/// Wrapper fino sobre <see cref="MongoConventions"/> (Es.Nimita.Infra.Mongo): el juego
/// <see cref="MongoConventionOptions.Default"/> es 1:1 el histórico de Wardkitten — camelCase +
/// IgnoreExtraElements + IgnoreIfNull + enums como string + decimal/decimal? como Decimal128, con
/// fechas UTC nativas del driver. Lo protege MongoConventionsGuardTests (forma BSON de producción).
/// </summary>
public static class MongoDbConfigurator
{
    public static void Configure() => MongoConventions.Register(MongoConventionOptions.Default);
}
