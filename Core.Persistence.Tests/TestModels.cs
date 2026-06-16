namespace Core.Persistence.Tests
{
    /// <summary>Side of a trade; used to exercise enum-as-string round-tripping.</summary>
    public enum Side
    {
        Buy,
        Sell,
    }

    /// <summary>Single-property string key (id member = <see cref="Symbol"/>), like the Beta store.</summary>
    public sealed class Beta
    {
        public string Symbol { get; set; } = string.Empty;

        public decimal Value { get; set; }
    }

    /// <summary>
    /// An <see cref="IDocument"/> with a money field, enum, <see cref="DateTimeOffset"/>, and nullable —
    /// the four fidelity-critical shapes from the migration design.
    /// </summary>
    public sealed class Trade : IDocument
    {
        public string Id { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal? Stop { get; set; }

        public Side Side { get; set; }

        public DateTimeOffset OpenedAt { get; set; }
    }

    /// <summary>Composite key (<c>{Date}_{Account}</c>), like the Sessions store.</summary>
    public sealed class Session
    {
        public string Date { get; set; } = string.Empty;

        public string Account { get; set; } = string.Empty;

        public int OrderCount { get; set; }

        public static string KeyOf(Session s) => $"{s.Date}_{s.Account}";
    }
}
