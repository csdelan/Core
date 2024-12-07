namespace Core
{
    public class Tags
    {
        public Tags() { TagsCollection = new HashSet<string>(); }
        public Tags(IEnumerable<string> tags) { TagsCollection = new HashSet<string>(tags); }

        public HashSet<string> TagsCollection { get; init; }
        public string TagString => " " + string.Join(" ", TagsCollection) + " ";
        public bool Contains(string tag) => TagsCollection.Contains(tag);
        public void AddTag(string tag) { TagsCollection.Add(tag); }
        public void RemoveTag(string tag) { TagsCollection.Remove(tag); }
        public override string ToString() { return TagString; }
    }
}
