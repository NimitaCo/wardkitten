using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Shouldly;
using Wardkitten.Infrastructure.Mongo;

namespace Wardkitten.Tests.Infrastructure;

/// <summary>
/// Candado de la forma BSON de PRODUCCIÓN. Wardkitten está desplegado con datos reales: si estas
/// aserciones cambian, la adopción/actualización de librerías ha alterado la serialización y hay que
/// PARAR (los documentos existentes dejarían de leerse/escribirse igual). Cubre el juego completo de
/// convenciones: camelCase, enum como string, decimal/decimal? como Decimal128, null no persistido,
/// campos extra tolerados y DateTime nativo del driver (UTC).
/// </summary>
public class MongoConventionsGuardTests
{
    private enum SampleState { None, ActiveRunning }

    private sealed class SampleDocument
    {
        public string Id { get; set; } = "sample";
        public SampleState CurrentState { get; set; }
        public decimal BalanceCredits { get; set; }
        public decimal? NullableAmount { get; set; }
        public string? OptionalNote { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    [Fact]
    public void Conventions_PreserveProductionBsonShape()
    {
        MongoDbConfigurator.Configure();

        var doc = new SampleDocument
        {
            CurrentState = SampleState.ActiveRunning,
            BalanceCredits = 12.5m,
            NullableAmount = 3.25m,
            OptionalNote = null,
            CreatedAtUtc = new DateTime(2026, 7, 28, 10, 30, 0, DateTimeKind.Utc),
        };

        var bson = doc.ToBsonDocument();

        // camelCase (PascalCase C# -> camelCase en Mongo) y `Id` mapeado a `_id`.
        bson.Names.ShouldBe(new[] { "_id", "currentState", "balanceCredits", "nullableAmount", "createdAtUtc" }, ignoreOrder: true);

        // Enum persistido como string, con el nombre del miembro tal cual (sin camelCase del valor).
        bson["currentState"].BsonType.ShouldBe(BsonType.String);
        bson["currentState"].AsString.ShouldBe("ActiveRunning");

        // decimal y decimal? como Decimal128 (numérico real: permite $inc atómico en la wallet).
        bson["balanceCredits"].BsonType.ShouldBe(BsonType.Decimal128);
        bson["balanceCredits"].AsDecimal.ShouldBe(12.5m);
        bson["nullableAmount"].BsonType.ShouldBe(BsonType.Decimal128);

        // Miembros null NO persistidos (documentos dispersos).
        bson.Contains("optionalNote").ShouldBeFalse();

        // DateTime nativo del driver: BSON DateTime en UTC (sin semántica legacy local<->UTC).
        bson["createdAtUtc"].BsonType.ShouldBe(BsonType.DateTime);
        bson["createdAtUtc"].ToUniversalTime().ShouldBe(doc.CreatedAtUtc);
    }

    [Fact]
    public void Conventions_IgnoreExtraElements_WhenDeserializingOldDocuments()
    {
        MongoDbConfigurator.Configure();

        var bson = new BsonDocument
        {
            ["_id"] = "sample",
            ["currentState"] = "ActiveRunning",
            ["balanceCredits"] = new BsonDecimal128(1m),
            ["campoAntiguoYaNoMapeado"] = "lo que sea",
        };

        var doc = BsonSerializer.Deserialize<SampleDocument>(bson);

        doc.Id.ShouldBe("sample");
        doc.CurrentState.ShouldBe(SampleState.ActiveRunning);
        doc.BalanceCredits.ShouldBe(1m);
        doc.NullableAmount.ShouldBeNull();
    }
}
