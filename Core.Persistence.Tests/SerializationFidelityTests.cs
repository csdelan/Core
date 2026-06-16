using MongoDB.Driver;

namespace Core.Persistence.Tests
{
    /// <summary>
    /// The non-negotiable fidelity gate: a value must be identical after JSON round-trip and after Mongo
    /// round-trip, with explicit coverage of enums, decimal money fields (Decimal128),
    /// <see cref="DateTimeOffset"/> (including offset), and nulls.
    /// </summary>
    [Collection(MongoCollection.Name)]
    public sealed class SerializationFidelityTests : IDisposable
    {
        private readonly MongoFixture _fx;
        private readonly string _dir;

        public SerializationFidelityTests(MongoFixture fx)
        {
            _fx = fx;
            _dir = Path.Combine(Path.GetTempPath(), "core-persist-fidelity", Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        private MongoDocumentStore<Trade> NewMongoStore()
        {
            MongoConventions.RegisterIdMap<Trade>(t => t.Id);
            IMongoCollection<Trade> collection = _fx.NewDatabase().GetCollection<Trade>("trades");
            return new MongoDocumentStore<Trade>(collection, t => t.Id);
        }

        public static IEnumerable<object[]> Cases()
        {
            yield return [new Trade
            {
                Id = "T1",
                Price = 123.456789m,
                Stop = 120.000001m,
                Side = Side.Buy,
                OpenedAt = new DateTimeOffset(2026, 6, 16, 9, 30, 0, TimeSpan.FromHours(-5)),
            }];
            yield return [new Trade
            {
                Id = "T2",
                Price = 0.0001m,
                Stop = null, // null money field
                Side = Side.Sell,
                OpenedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            }];
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public async Task JsonAndMongo_RoundTrip_AreValueIdentical(Trade original)
        {
            // JSON: object -> file -> object
            var jsonStore = new JsonDocumentStore<Trade>(_dir, t => t.Id);
            await jsonStore.SaveAsync(original);
            Trade? fromJson = await jsonStore.GetAsync(original.Id);

            // Mongo: object -> collection -> object
            MongoDocumentStore<Trade> mongoStore = NewMongoStore();
            await mongoStore.SaveAsync(original);
            Trade? fromMongo = await mongoStore.GetAsync(original.Id);

            AssertTradeEqual(original, fromJson);
            AssertTradeEqual(original, fromMongo);
        }

        private static void AssertTradeEqual(Trade expected, Trade? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual!.Id);
            Assert.Equal(expected.Side, actual.Side);

            // decimal must be exact (Decimal128 / decimal, never binary double drift).
            Assert.Equal(expected.Price, actual.Price);
            Assert.Equal(expected.Stop, actual.Stop);

            // DateTimeOffset must preserve both the instant and the original offset.
            Assert.Equal(expected.OpenedAt, actual.OpenedAt);
            Assert.Equal(expected.OpenedAt.Offset, actual.OpenedAt.Offset);
        }
    }
}
