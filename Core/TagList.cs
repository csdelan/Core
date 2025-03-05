using System.Text;

namespace Core
{
    public interface ITaggable
    {
        public TagList Tags { get; set; }
    }

    public class TagList : HashSet<string>
    {
        public TagList() 
        { }

        public TagList(string[] tags)
        {
            this.UnionWith(tags);
        }

        public TagList(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                // If the input string is null, empty, or whitespace, initialize an empty set.
                return;
            }

            try
            {
                // Split tags by spaces and trim entries to ensure clean tags.
                string[] substrings = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                this.UnionWith(substrings);
            }
            catch (Exception ex)
            {
                // Log or handle unexpected errors gracefully.
                // Consider using a logging framework or re-throwing a custom exception.
                throw new ArgumentException("Invalid input for tags. Ensure the input is a valid space-separated string.", ex);
            }
        }

        public override string ToString() 
        {
            StringBuilder sb = new();
            foreach(string tag in this)
            {
                sb.Append(tag);
                sb.Append(' ');
            }
            return sb.ToString().TrimEnd(' ');
        }
    }
}
