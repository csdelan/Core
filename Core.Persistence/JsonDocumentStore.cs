using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Persistence
{
    /// <summary>
    /// An <see cref="IDocumentStore{T}"/> backed by one JSON file per entity under a directory.
    /// </summary>
    /// <typeparam name="T">The aggregate type stored as <c>{id}.json</c>.</typeparam>
    /// <remarks>
    /// <para>
    /// Writes are atomic (temp file then rename) with a short retry loop, which survives the transient
    /// <see cref="IOException"/> / <see cref="UnauthorizedAccessException"/> races caused by cloud-sync,
    /// antivirus, and search-indexer processes touching the directory.
    /// </para>
    /// <para>
    /// This is the drop-in twin of <see cref="MongoDocumentStore{T}"/>: identical behaviour behind the
    /// same interface, which is what makes per-store JSON ⇄ Mongo toggling and instant fallback possible.
    /// Queries enumerate and deserialize the directory and evaluate the predicate in memory.
    /// </para>
    /// </remarks>
    public sealed class JsonDocumentStore<T> : IDocumentStore<T>
        where T : class
    {
        private const int MaxWriteAttempts = 5;

        private readonly string _directory;
        private readonly Func<T, string> _idSelector;
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Creates a store rooted at <paramref name="directory"/>.
        /// </summary>
        /// <param name="directory">The directory that holds the entity files.</param>
        /// <param name="idSelector">Extracts an entity's string key (used as the file name).</param>
        /// <param name="options">
        /// Serializer options. Defaults to web conventions plus a string enum converter to match the
        /// established JSON store format.
        /// </param>
        public JsonDocumentStore(
            string directory,
            Func<T, string> idSelector,
            JsonSerializerOptions? options = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(directory);
            ArgumentNullException.ThrowIfNull(idSelector);
            _directory = directory;
            _idSelector = idSelector;
            _options = options ?? CreateDefaultOptions();
        }

        /// <summary>Builds the default serializer options (web defaults + string enums + indentation).</summary>
        public static JsonSerializerOptions CreateDefaultOptions() => new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <inheritdoc />
        public async Task<T?> GetAsync(string id, CancellationToken ct = default)
        {
            string path = PathFor(DocumentKey.Validate(id));
            if (!File.Exists(path))
                return null;

            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SaveAsync(T entity, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(entity);
            string path = PathFor(DocumentKey.Validate(_idSelector(entity), nameof(entity)));
            Directory.CreateDirectory(_directory);

            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(entity, _options);
            await WriteAtomicAsync(path, payload, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
        {
            string path = PathFor(DocumentKey.Validate(id));
            if (!File.Exists(path))
                return Task.FromResult(false);

            File.Delete(path);
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<T>> QueryAsync(
            Expression<Func<T, bool>> filter,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(filter);
            if (!Directory.Exists(_directory))
                return [];

            Func<T, bool> predicate = filter.Compile();
            var results = new List<T>();
            foreach (string file in Directory.EnumerateFiles(_directory, "*.json"))
            {
                ct.ThrowIfCancellationRequested();
                await using FileStream stream = File.OpenRead(file);
                T? entity = await JsonSerializer.DeserializeAsync<T>(stream, _options, ct).ConfigureAwait(false);
                if (entity is not null && predicate(entity))
                    results.Add(entity);
            }

            return results;
        }

        private string PathFor(string id)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                if (id.Contains(invalid))
                {
                    throw new ArgumentException(
                        $"Document key '{id}' contains character(s) that are invalid in a file name.",
                        nameof(id));
                }
            }

            return Path.Combine(_directory, id + ".json");
        }

        private static async Task WriteAtomicAsync(string path, byte[] payload, CancellationToken ct)
        {
            string tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllBytesAsync(tempPath, payload, ct).ConfigureAwait(false);

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    File.Move(tempPath, path, overwrite: true);
                    return;
                }
                catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException)
                                           && attempt < MaxWriteAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), ct).ConfigureAwait(false);
                }
                catch
                {
                    TryDelete(tempPath);
                    throw;
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup of the temp file; ignore.
            }
        }
    }
}
