using System.Linq.Expressions;
using MongoDB.Driver;

namespace Core.Persistence
{
    /// <summary>
    /// An <see cref="IDocumentStore{T}"/> backed by a single MongoDB collection (one document per entity).
    /// </summary>
    /// <typeparam name="T">The aggregate type stored in the collection.</typeparam>
    /// <remarks>
    /// <para>
    /// Upserts use an atomic server-side <c>ReplaceOne</c> keyed on <c>_id</c>, which replaces the
    /// per-process locking that a file store needs. Queries are translated to real server-side filters
    /// rather than enumerating and deserializing every document.
    /// </para>
    /// <para>
    /// The collection's <c>_id</c> mapping must be registered (see
    /// <see cref="MongoConventions.RegisterIdMap{T}"/>) so the string key produced by the id selector
    /// matches the stored <c>_id</c>. The <see cref="DocumentStoreFactory"/> wires this up automatically.
    /// </para>
    /// </remarks>
    public sealed class MongoDocumentStore<T> : IDocumentStore<T>
        where T : class
    {
        private readonly IMongoCollection<T> _collection;
        private readonly Func<T, string> _idSelector;

        /// <summary>
        /// Creates a store over the supplied collection.
        /// </summary>
        /// <param name="collection">The MongoDB collection for <typeparamref name="T"/>.</param>
        /// <param name="idSelector">Extracts an entity's string key (mapped to <c>_id</c>).</param>
        public MongoDocumentStore(IMongoCollection<T> collection, Func<T, string> idSelector)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(idSelector);
            _collection = collection;
            _idSelector = idSelector;
        }

        private static FilterDefinition<T> ById(string id) => Builders<T>.Filter.Eq("_id", id);

        /// <inheritdoc />
        public async Task<T?> GetAsync(string id, CancellationToken ct = default)
        {
            DocumentKey.Validate(id);
            return await _collection.Find(ById(id)).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SaveAsync(T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            string id = DocumentKey.Validate(_idSelector(entity), nameof(entity));
            await _collection
                .ReplaceOneAsync(ById(id), entity, new ReplaceOptions { IsUpsert = true }, ct)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
        {
            DocumentKey.Validate(id);
            DeleteResult result = await _collection.DeleteOneAsync(ById(id), ct).ConfigureAwait(false);
            return result.DeletedCount > 0;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<T>> QueryAsync(
            Expression<Func<T, bool>> filter,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(filter);
            return await _collection.Find(filter).ToListAsync(ct).ConfigureAwait(false);
        }
    }
}
