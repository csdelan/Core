using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Core.Persistence
{
    /// <summary>
    /// Default <see cref="IDocumentStoreFactory"/> that selects the backend from <see cref="PersistenceOptions"/>.
    /// </summary>
    /// <remarks>
    /// The MongoDB client is resolved lazily and only when a Mongo-backed store is actually created, so a
    /// JSON-only configuration never requires a running <c>mongod</c> and the database being down at
    /// startup does not prevent the application from starting.
    /// </remarks>
    public sealed class DocumentStoreFactory : IDocumentStoreFactory
    {
        private readonly PersistenceOptions _options;
        private readonly IServiceProvider _services;

        /// <summary>Creates the factory.</summary>
        /// <param name="options">The bound persistence options.</param>
        /// <param name="services">The provider used to resolve the Mongo client on demand.</param>
        public DocumentStoreFactory(IOptions<PersistenceOptions> options, IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(services);
            _options = options.Value;
            _services = services;
        }

        /// <inheritdoc />
        public IDocumentStore<T> Create<T>(
            string storeName,
            string collectionName,
            string jsonSubDirectory,
            Expression<Func<T, object>> idMember)
            where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
            ArgumentNullException.ThrowIfNull(idMember);

            Func<T, string> selector = DocumentKey.SelectorFromExpression(idMember);

            return _options.BackendFor(storeName) switch
            {
                StoreBackend.Mongo => CreateMongo(collectionName, idMember, selector),
                _ => CreateJson(jsonSubDirectory, selector),
            };
        }

        /// <inheritdoc />
        public IDocumentStore<T> Create<T>(string storeName, string collectionName, string jsonSubDirectory)
            where T : class, IDocument
        {
            Expression<Func<T, object>> idMember = x => x.Id;
            return Create(storeName, collectionName, jsonSubDirectory, idMember);
        }

        private IDocumentStore<T> CreateJson<T>(string jsonSubDirectory, Func<T, string> selector)
            where T : class
        {
            string directory = string.IsNullOrWhiteSpace(jsonSubDirectory)
                ? _options.JsonRootPath
                : Path.Combine(_options.JsonRootPath, jsonSubDirectory);
            return new JsonDocumentStore<T>(directory, selector);
        }

        private IDocumentStore<T> CreateMongo<T>(
            string collectionName,
            Expression<Func<T, object>> idMember,
            Func<T, string> selector)
            where T : class
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
            MongoConventions.RegisterIdMap(idMember);

            IMongoClient client = _services.GetRequiredService<IMongoClient>();
            IMongoDatabase database = client.GetDatabase(_options.Mongo.DatabaseName);
            IMongoCollection<T> collection = database.GetCollection<T>(collectionName);
            return new MongoDocumentStore<T>(collection, selector);
        }
    }
}
