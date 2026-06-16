using System.Linq.Expressions;

namespace Core.Persistence
{
    /// <summary>
    /// Creates <see cref="IDocumentStore{T}"/> instances, choosing the JSON or MongoDB backend per
    /// <see cref="PersistenceOptions"/>.
    /// </summary>
    /// <remarks>
    /// This is the seam that realizes the per-store toggle: the same call returns a JSON-backed or a
    /// Mongo-backed store depending only on configuration, so callers and consumers never change.
    /// </remarks>
    public interface IDocumentStoreFactory
    {
        /// <summary>
        /// Creates a store for <typeparamref name="T"/>, resolving the key from an id-member expression.
        /// </summary>
        /// <typeparam name="T">The aggregate type.</typeparam>
        /// <param name="storeName">The logical store name used to look up the configured backend.</param>
        /// <param name="collectionName">The MongoDB collection name (used when the backend is Mongo).</param>
        /// <param name="jsonSubDirectory">
        /// The sub-directory under <see cref="PersistenceOptions.JsonRootPath"/> (used when the backend is JSON).
        /// </param>
        /// <param name="idMember">An expression selecting the id member, e.g. <c>x =&gt; x.Symbol</c>.</param>
        /// <returns>A store bound to the configured backend for <paramref name="storeName"/>.</returns>
        IDocumentStore<T> Create<T>(
            string storeName,
            string collectionName,
            string jsonSubDirectory,
            Expression<Func<T, object>> idMember)
            where T : class;

        /// <summary>
        /// Creates a store for an <see cref="IDocument"/> type, defaulting the key to <c>x =&gt; x.Id</c>.
        /// </summary>
        /// <typeparam name="T">The aggregate type implementing <see cref="IDocument"/>.</typeparam>
        /// <param name="storeName">The logical store name used to look up the configured backend.</param>
        /// <param name="collectionName">The MongoDB collection name (used when the backend is Mongo).</param>
        /// <param name="jsonSubDirectory">
        /// The sub-directory under <see cref="PersistenceOptions.JsonRootPath"/> (used when the backend is JSON).
        /// </param>
        /// <returns>A store bound to the configured backend for <paramref name="storeName"/>.</returns>
        IDocumentStore<T> Create<T>(string storeName, string collectionName, string jsonSubDirectory)
            where T : class, IDocument;
    }
}
