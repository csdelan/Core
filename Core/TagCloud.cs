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

        public List<KeyValuePair<string,int>> GetTagStatistics()
        {
            Dictionary<string, int> tagStatistics = [];
            foreach (ITaggable taggable in this.objectList)
            {
                foreach (string tag in taggable.Tags)
                {
                    if (tagStatistics.TryGetValue(tag, out int value))
                    {
                        tagStatistics[tag]=++value;
                    }
                    else
                    {
                        tagStatistics[tag] = 1;
                    }
                }
            }
            var sortedDict = from entry in tagStatistics orderby entry.Value descending select entry;
            return [.. sortedDict];
        }
    }
}
