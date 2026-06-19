using System.Text;

namespace Core
{
    /// <summary>
    /// Base for time-stamped documents persisted via <see cref="IDocumentStore{T}"/> (JSON file DB or
    /// MongoDB). Derived types supply a stable, filesystem-safe <see cref="IDocument.Id"/> — typically
    /// a composite of <see cref="Timestamp"/> and a series key built with <see cref="Slug"/>.
    /// </summary>
    public abstract record TimeSeriesDocument : IDocument
    {
        /// <summary>Stable, filesystem-safe document key.</summary>
        public abstract string Id { get; }

        /// <summary>The instant this observation belongs to (UTC canonical).</summary>
        public required DateTimeOffset Timestamp { get; init; }

        /// <summary>Collapses arbitrary text to a key segment safe for file names and Mongo ids.</summary>
        protected static string Slug(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value.Trim())
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-');

            return sb.ToString().Trim('-');
        }
    }
}
