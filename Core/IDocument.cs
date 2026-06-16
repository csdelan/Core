namespace Core
{
    /// <summary>
    /// Optional marker for entities that expose their own string identity through an <see cref="Id"/>
    /// property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementing this interface lets an entity be registered with an <see cref="IDocumentStore{T}"/>
    /// without specifying an id selector — the store defaults to <c>x =&gt; x.Id</c>.
    /// </para>
    /// <para>
    /// This is purely a convenience for types you own and design to be persistence-ready. It is never
    /// required: external or sealed models that cannot implement this interface are supported by
    /// supplying an id selector (an id-member expression or an explicit <see cref="System.Func{T, TResult}"/>)
    /// when the store is created.
    /// </para>
    /// </remarks>
    public interface IDocument
    {
        /// <summary>
        /// Gets the stable string identity of this entity. Must not be <c>null</c> or whitespace when
        /// the entity is persisted.
        /// </summary>
        string Id { get; }
    }
}
