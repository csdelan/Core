using System.Linq.Expressions;

namespace Core
{
    /// <summary>
    /// A minimal, backend-agnostic document repository for a single aggregate type.
    /// </summary>
    /// <typeparam name="T">The reference type stored as one document per entity.</typeparam>
    /// <remarks>
    /// <para>
    /// This is the integration seam that lets a persisted object be served either from a JSON
    /// file store or from a MongoDB collection without the consumer changing. Each entity maps to
    /// exactly one document (one file, or one collection document), keyed by a string id.
    /// </para>
    /// <para>
    /// The id is not part of this contract's method signatures for <see cref="SaveAsync"/>: the
    /// concrete store is configured with an id selector (typically derived from a single id member)
    /// so it can extract the key from the entity itself. See the implementations in
    /// <c>Core.Persistence</c> for how the selector and the storage key are kept in lockstep.
    /// </para>
    /// <para>
    /// All operations are asynchronous and honour the supplied <see cref="CancellationToken"/>.
    /// Implementations are expected to be safe for concurrent use across multiple callers.
    /// </para>
    /// </remarks>
    public interface IDocumentStore<T> where T : class
    {
        /// <summary>
        /// Retrieves a single entity by its string id.
        /// </summary>
        /// <param name="id">The entity key. Must not be <c>null</c> or whitespace.</param>
        /// <param name="ct">A token to observe for cancellation.</param>
        /// <returns>The stored entity, or <c>null</c> if no document exists with that id.</returns>
        Task<T?> GetAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Inserts or replaces the document for <paramref name="entity"/> (an upsert).
        /// </summary>
        /// <param name="entity">The entity to persist. Its id is resolved by the store's id selector.</param>
        /// <param name="ct">A token to observe for cancellation.</param>
        /// <remarks>
        /// The operation is keyed by the entity's resolved id: an existing document with the same id
        /// is replaced, otherwise a new document is created.
        /// </remarks>
        Task SaveAsync(T entity, CancellationToken ct = default);

        /// <summary>
        /// Deletes the document with the specified id.
        /// </summary>
        /// <param name="id">The entity key. Must not be <c>null</c> or whitespace.</param>
        /// <param name="ct">A token to observe for cancellation.</param>
        /// <returns><c>true</c> if a document existed and was deleted; otherwise <c>false</c>.</returns>
        Task<bool> DeleteAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Returns all entities matching the supplied predicate.
        /// </summary>
        /// <param name="filter">A predicate expression evaluated against each stored entity.</param>
        /// <param name="ct">A token to observe for cancellation.</param>
        /// <returns>The matching entities; an empty list if none match.</returns>
        /// <remarks>
        /// A document-database implementation translates the expression into a server-side query; a
        /// file-based implementation compiles and evaluates it in memory. Callers should treat the
        /// result as a point-in-time snapshot.
        /// </remarks>
        Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
    }
}
