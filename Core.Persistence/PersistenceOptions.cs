namespace Core.Persistence
{
    /// <summary>
    /// Identifies which storage backend a document store uses.
    /// </summary>
    public enum StoreBackend
    {
        /// <summary>One JSON file per document under a directory.</summary>
        Json,

        /// <summary>One document per entity in a MongoDB collection.</summary>
        Mongo,
    }

    /// <summary>
    /// MongoDB connection settings.
    /// </summary>
    public sealed class MongoOptions
    {
        /// <summary>
        /// The MongoDB connection string. Defaults to a local node so the order/data path never depends
        /// on a cloud round trip.
        /// </summary>
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";

        /// <summary>The database that holds the document collections.</summary>
        public string DatabaseName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration for the persistence layer, bound from the <c>Persistence</c> configuration section.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each logical store can be flipped between <see cref="StoreBackend.Json"/> and
    /// <see cref="StoreBackend.Mongo"/> independently via <see cref="Stores"/>, with
    /// <see cref="DefaultBackend"/> applied to any store not named explicitly. This enables incremental,
    /// per-store cutover with instant fallback.
    /// </para>
    /// <example>
    /// <code>
    /// "Persistence": {
    ///   "Mongo": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "TradingSystem" },
    ///   "JsonRootPath": "C:\\Users\\me\\OneDrive\\TradingSystem",
    ///   "DefaultBackend": "Json",
    ///   "Stores": { "Beta": "Mongo", "Journal": "Mongo", "Trade": "Json" }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public sealed class PersistenceOptions
    {
        /// <summary>The default configuration section name.</summary>
        public const string SectionName = "Persistence";

        /// <summary>MongoDB connection settings (used only by Mongo-backed stores).</summary>
        public MongoOptions Mongo { get; set; } = new();

        /// <summary>
        /// The root directory under which JSON stores create their per-store sub-directories.
        /// </summary>
        public string JsonRootPath { get; set; } = string.Empty;

        /// <summary>The backend used for any store not listed in <see cref="Stores"/>.</summary>
        public StoreBackend DefaultBackend { get; set; } = StoreBackend.Json;

        /// <summary>Per-store backend overrides, keyed by logical store name (case-insensitive).</summary>
        public Dictionary<string, StoreBackend> Stores { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Resolves the backend for a logical store, falling back to <see cref="DefaultBackend"/>.
        /// </summary>
        /// <param name="storeName">The logical store name (e.g. "Beta", "Trade").</param>
        /// <returns>The configured backend for that store.</returns>
        public StoreBackend BackendFor(string storeName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
            return Stores.TryGetValue(storeName, out StoreBackend backend) ? backend : DefaultBackend;
        }
    }
}
