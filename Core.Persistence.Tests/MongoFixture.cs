using EphemeralMongo;
using MongoDB.Driver;

namespace Core.Persistence.Tests
{
    /// <summary>
    /// Starts a single throwaway <c>mongod</c> for the whole Mongo test collection and hands out a fresh
    /// database per test for isolation.
    /// </summary>
    public sealed class MongoFixture : IAsyncLifetime
    {
        private IMongoRunner? _runner;

        public IMongoClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            _runner = await MongoRunner.RunAsync(new MongoRunnerOptions
            {
                StandardOutputLogger = _ => { },
                StandardErrorLogger = _ => { },
            });
            Client = new MongoClient(_runner.ConnectionString);
        }

        public Task DisposeAsync()
        {
            _runner?.Dispose();
            return Task.CompletedTask;
        }

        /// <summary>Returns a uniquely-named database so tests never see each other's data.</summary>
        public IMongoDatabase NewDatabase() =>
            Client.GetDatabase("t_" + Guid.NewGuid().ToString("N"));
    }

    /// <summary>xUnit collection that shares one <see cref="MongoFixture"/> across all Mongo tests.</summary>
    [CollectionDefinition(Name)]
    public sealed class MongoCollection : ICollectionFixture<MongoFixture>
    {
        public const string Name = "mongo";
    }
}
