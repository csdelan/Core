using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Core.Persistence
{
    /// <summary>
    /// Dependency-injection helpers for registering the persistence layer and individual document stores.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="PersistenceOptions"/>, a lazily-connected <see cref="IMongoClient"/>, and the
        /// <see cref="IDocumentStoreFactory"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Configuration containing the <c>Persistence</c> section.</param>
        /// <param name="sectionName">The section name to bind (defaults to <c>Persistence</c>).</param>
        /// <returns>The same service collection, for chaining.</returns>
        /// <remarks>
        /// The <see cref="IMongoClient"/> is created from the connection string but does not connect until
        /// first use, so this is safe to call regardless of whether MongoDB is currently reachable.
        /// </remarks>
        public static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = PersistenceOptions.SectionName)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.Configure<PersistenceOptions>(configuration.GetSection(sectionName));

            services.AddSingleton<IMongoClient>(sp =>
            {
                PersistenceOptions options = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
                return new MongoClient(options.Mongo.ConnectionString);
            });

            services.AddSingleton<IDocumentStoreFactory, DocumentStoreFactory>();
            return services;
        }

        /// <summary>
        /// Registers a singleton <see cref="IDocumentStore{T}"/> resolved through the factory, with an
        /// id-member expression.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="storeName">The logical store name used to look up the configured backend.</param>
        /// <param name="collectionName">The MongoDB collection name.</param>
        /// <param name="jsonSubDirectory">The JSON sub-directory under the configured root.</param>
        /// <param name="idMember">An expression selecting the id member.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddDocumentStore<T>(
            this IServiceCollection services,
            string storeName,
            string collectionName,
            string jsonSubDirectory,
            Expression<Func<T, object>> idMember)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IDocumentStore<T>>(sp =>
                sp.GetRequiredService<IDocumentStoreFactory>()
                    .Create(storeName, collectionName, jsonSubDirectory, idMember));
            return services;
        }

        /// <summary>
        /// Registers a singleton <see cref="IDocumentStore{T}"/> for an <see cref="IDocument"/> type,
        /// defaulting the key to <c>x =&gt; x.Id</c>.
        /// </summary>
        /// <typeparam name="T">The aggregate type implementing <see cref="IDocument"/>.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="storeName">The logical store name used to look up the configured backend.</param>
        /// <param name="collectionName">The MongoDB collection name.</param>
        /// <param name="jsonSubDirectory">The JSON sub-directory under the configured root.</param>
        /// <returns>The same service collection, for chaining.</returns>
        public static IServiceCollection AddDocumentStore<T>(
            this IServiceCollection services,
            string storeName,
            string collectionName,
            string jsonSubDirectory)
            where T : class, IDocument
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddSingleton<IDocumentStore<T>>(sp =>
                sp.GetRequiredService<IDocumentStoreFactory>()
                    .Create<T>(storeName, collectionName, jsonSubDirectory));
            return services;
        }
    }
}
