using MongoDB.Driver;

namespace Core.Persistence.Tests
{
    /// <summary>
    /// Exercises <see cref="MongoDocumentStore{T}"/> against a real (ephemeral) MongoDB server.
    /// </summary>
    [Collection(MongoCollection.Name)]
    public sealed class MongoDocumentStoreTests
    {
        private readonly MongoFixture _fx;

        public MongoDocumentStoreTests(MongoFixture fx) => _fx = fx;

        private MongoDocumentStore<Beta> NewBetaStore()
        {
            MongoConventions.RegisterIdMap<Beta>(b => b.Symbol);
            IMongoCollection<Beta> collection = _fx.NewDatabase().GetCollection<Beta>("betas");
            return new MongoDocumentStore<Beta>(collection, b => b.Symbol);
        }

        [Fact]
        public async Task SaveAsync_ThenGetAsync_RoundTrips()
        {
            MongoDocumentStore<Beta> store = NewBetaStore();

            await store.SaveAsync(new Beta { Symbol = "AAPL", Value = 1.23m });
            Beta? loaded = await store.GetAsync("AAPL");

            Assert.NotNull(loaded);
            Assert.Equal("AAPL", loaded!.Symbol);
            Assert.Equal(1.23m, loaded.Value);
        }

        [Fact]
        public async Task GetAsync_MissingDocument_ReturnsNull()
        {
            MongoDocumentStore<Beta> store = NewBetaStore();

            Assert.Null(await store.GetAsync("MISSING"));
        }

        [Fact]
        public async Task SaveAsync_SameId_UpsertsWithoutDuplicating()
        {
            MongoDocumentStore<Beta> store = NewBetaStore();

            await store.SaveAsync(new Beta { Symbol = "MSFT", Value = 1m });
            await store.SaveAsync(new Beta { Symbol = "MSFT", Value = 2m });

            IReadOnlyList<Beta> all = await store.QueryAsync(_ => true);
            Assert.Single(all);
            Assert.Equal(2m, all[0].Value);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrueThenFalse()
        {
            MongoDocumentStore<Beta> store = NewBetaStore();
            await store.SaveAsync(new Beta { Symbol = "NVDA", Value = 5m });

            Assert.True(await store.DeleteAsync("NVDA"));
            Assert.False(await store.DeleteAsync("NVDA"));
            Assert.Null(await store.GetAsync("NVDA"));
        }

        [Fact]
        public async Task QueryAsync_TranslatesPredicateServerSide()
        {
            MongoDocumentStore<Beta> store = NewBetaStore();
            await store.SaveAsync(new Beta { Symbol = "A", Value = 1m });
            await store.SaveAsync(new Beta { Symbol = "B", Value = 10m });
            await store.SaveAsync(new Beta { Symbol = "C", Value = 100m });

            IReadOnlyList<Beta> big = await store.QueryAsync(b => b.Value >= 10m);

            Assert.Equal(2, big.Count);
            Assert.DoesNotContain(big, b => b.Symbol == "A");
        }
    }
}
