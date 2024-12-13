using System.Collections;
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
