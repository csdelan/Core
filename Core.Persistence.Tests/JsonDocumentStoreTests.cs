namespace Core.Persistence.Tests
{
    /// <summary>
    /// Exercises <see cref="JsonDocumentStore{T}"/> against a real temp directory.
    /// </summary>
    public sealed class JsonDocumentStoreTests : IDisposable
    {
        private readonly string _dir;

        public JsonDocumentStoreTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "core-persist-tests", Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        private JsonDocumentStore<Beta> NewBetaStore() =>
            new(_dir, b => b.Symbol);

        [Fact]
        public async Task GetAsync_MissingDocument_ReturnsNull()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();

            Beta? result = await store.GetAsync("NOPE");

            Assert.Null(result);
        }

        [Fact]
        public async Task SaveAsync_ThenGetAsync_RoundTripsValues()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();
            var beta = new Beta { Symbol = "AAPL", Value = 1.23m };

            await store.SaveAsync(beta);
            Beta? loaded = await store.GetAsync("AAPL");

            Assert.NotNull(loaded);
            Assert.Equal("AAPL", loaded!.Symbol);
            Assert.Equal(1.23m, loaded.Value);
        }

        [Fact]
        public async Task SaveAsync_SameId_UpsertsInPlace()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();

            await store.SaveAsync(new Beta { Symbol = "MSFT", Value = 1m });
            await store.SaveAsync(new Beta { Symbol = "MSFT", Value = 2m });

            IReadOnlyList<Beta> all = await store.QueryAsync(_ => true);
            Assert.Single(all);
            Assert.Equal(2m, all[0].Value);
        }

        [Fact]
        public async Task SaveAsync_LeavesNoTempFileResidue()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();

            await store.SaveAsync(new Beta { Symbol = "TSLA", Value = 9m });

            string[] tempFiles = Directory.GetFiles(_dir, "*.tmp");
            Assert.Empty(tempFiles);
            Assert.Single(Directory.GetFiles(_dir, "*.json"));
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrueThenFalse()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();
            await store.SaveAsync(new Beta { Symbol = "NVDA", Value = 5m });

            Assert.True(await store.DeleteAsync("NVDA"));
            Assert.False(await store.DeleteAsync("NVDA"));
            Assert.Null(await store.GetAsync("NVDA"));
        }

        [Fact]
        public async Task QueryAsync_AppliesPredicate()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();
            await store.SaveAsync(new Beta { Symbol = "A", Value = 1m });
            await store.SaveAsync(new Beta { Symbol = "B", Value = 10m });
            await store.SaveAsync(new Beta { Symbol = "C", Value = 100m });

            IReadOnlyList<Beta> big = await store.QueryAsync(b => b.Value >= 10m);

            Assert.Equal(2, big.Count);
            Assert.DoesNotContain(big, b => b.Symbol == "A");
        }

        [Fact]
        public async Task QueryAsync_EmptyDirectory_ReturnsEmpty()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();

            IReadOnlyList<Beta> all = await store.QueryAsync(_ => true);

            Assert.Empty(all);
        }

        [Fact]
        public async Task CompositeKey_RoundTrips()
        {
            var store = new JsonDocumentStore<Session>(_dir, Session.KeyOf);
            var session = new Session { Date = "2026-06-16", Account = "ACC1", OrderCount = 7 };

            await store.SaveAsync(session);
            Session? loaded = await store.GetAsync("2026-06-16_ACC1");

            Assert.NotNull(loaded);
            Assert.Equal(7, loaded!.OrderCount);
        }

        [Fact]
        public async Task SaveAsync_EmptyKey_Throws()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();

            await Assert.ThrowsAsync<ArgumentException>(
                () => store.SaveAsync(new Beta { Symbol = "  ", Value = 1m }));
        }

        [Fact]
        public async Task GetAsync_KeyWithPathSeparator_Throws()
        {
            JsonDocumentStore<Beta> store = NewBetaStore();

            await Assert.ThrowsAsync<ArgumentException>(() => store.GetAsync("a/b"));
        }
    }
}
