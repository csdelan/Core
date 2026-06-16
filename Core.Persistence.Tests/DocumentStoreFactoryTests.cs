using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Core.Persistence.Tests
{
    /// <summary>
    /// Verifies the factory honours the per-store backend toggle. No live server is needed: creating a
    /// <see cref="MongoClient"/> and getting a collection does not open a connection.
    /// </summary>
    public sealed class DocumentStoreFactoryTests
    {
        private static DocumentStoreFactory NewFactory(PersistenceOptions options)
        {
            ServiceProvider services = new ServiceCollection()
                .AddSingleton<IMongoClient>(new MongoClient("mongodb://localhost:27017"))
                .BuildServiceProvider();
            return new DocumentStoreFactory(Options.Create(options), services);
        }

        private static PersistenceOptions NewOptions() => new()
        {
            JsonRootPath = Path.GetTempPath(),
            DefaultBackend = StoreBackend.Json,
            Mongo = new MongoOptions { DatabaseName = "test" },
            Stores =
            {
                ["Beta"] = StoreBackend.Mongo,
                ["Trade"] = StoreBackend.Json,
            },
        };

        [Fact]
        public void Create_MongoConfiguredStore_ReturnsMongoStore()
        {
            DocumentStoreFactory factory = NewFactory(NewOptions());

            IDocumentStore<Beta> store = factory.Create<Beta>("Beta", "betas", "betas", b => b.Symbol);

            Assert.IsType<MongoDocumentStore<Beta>>(store);
        }

        [Fact]
        public void Create_JsonConfiguredStore_ReturnsJsonStore()
        {
            DocumentStoreFactory factory = NewFactory(NewOptions());

            IDocumentStore<Trade> store = factory.Create<Trade>("Trade", "trades", "trades");

            Assert.IsType<JsonDocumentStore<Trade>>(store);
        }

        [Fact]
        public void Create_UnlistedStore_FallsBackToDefaultBackend()
        {
            DocumentStoreFactory factory = NewFactory(NewOptions());

            IDocumentStore<Session> store =
                factory.Create<Session>("Sessions", "sessions", "sessions", s => s.Date + "_" + s.Account);

            Assert.IsType<JsonDocumentStore<Session>>(store);
        }

        [Fact]
        public void Create_IDocumentOverloadAndExpression_ResolveSameKey()
        {
            // The IDocument overload defaults to x => x.Id; confirm it matches an explicit expression.
            DocumentStoreFactory factory = NewFactory(new PersistenceOptions
            {
                JsonRootPath = Path.GetTempPath(),
                DefaultBackend = StoreBackend.Json,
            });

            IDocumentStore<Trade> viaInterface = factory.Create<Trade>("X", "x", "x");
            IDocumentStore<Trade> viaExpression = factory.Create<Trade>("X", "x", "x", t => t.Id);

            Assert.IsType<JsonDocumentStore<Trade>>(viaInterface);
            Assert.IsType<JsonDocumentStore<Trade>>(viaExpression);
        }
    }
}
