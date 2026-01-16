namespace Core.GoogleSheets
{
    /// <summary>
    /// Marks the key property used to identify/update/delete a row.
    /// Only one key is supported for simplicity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SheetKeyAttribute : Attribute { }
}
