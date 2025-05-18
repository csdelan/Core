namespace Core
{
    public class TagCloud(List<ITaggable>? objectList)
    {
        private readonly List<ITaggable> objectList = objectList ?? throw new ArgumentNullException(nameof(objectList));

        public List<string> GetAllTags()
        {
            TagList tags = [];
            foreach (ITaggable taggable in this.objectList)
            {
                foreach (string tag in taggable.Tags)
                {
                    tags.Add(tag);
                }
            }
            return [.. tags];
        }

        public List<KeyValuePair<string, int>> GetTagStatistics()
        {
            Dictionary<string, int> tagStatistics = new();
            foreach (ITaggable taggable in objectList)
            {
                foreach (string tag in taggable.Tags)
                {
                    tagStatistics[tag] = tagStatistics.GetValueOrDefault(tag) + 1;
                }
            }
            return tagStatistics.OrderByDescending(x => x.Value).ToList();
        }
    }
}
