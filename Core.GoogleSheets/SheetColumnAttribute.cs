namespace Core.GoogleSheets
{
    /// <summary>
    /// Maps a property to a column header (required).
    /// Optional Index lets you pin an explicit order (0-based).
    /// If Index is omitted, columns are ordered by property order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SheetColumnAttribute : Attribute
    {
        public string Header { get; }
        public int? Index { get; }
        public SheetColumnAttribute(string header)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
        }
        public SheetColumnAttribute(string header, int index)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Index = index;
        }
    }
}
