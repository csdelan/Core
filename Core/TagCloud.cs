namespace Core
{
    public class TagCloud
    {
        private List<ITaggable> objectList;

        public TagCloud(List<ITaggable> objectList)
        {
            this.objectList = objectList;
        }

        public List<string> GetAllTags()
        {
            TagList tags = new TagList();
            foreach (ITaggable taggable in this.objectList)
            {
                foreach (string tag in taggable.Tags)
                {
                    tags.Add(tag);
                }
            }
            return tags.ToList();
        }

        public List<KeyValuePair<string,int>> GetTagStatistics()
        {
            Dictionary<string, int> tagStatistics = new Dictionary<string, int>();
            foreach (ITaggable taggable in this.objectList)
            {
                foreach (string tag in taggable.Tags)
                {
                    if (tagStatistics.ContainsKey(tag))
                    {
                        tagStatistics[tag]++;
                    }
                    else
                    {
                        tagStatistics[tag] = 1;
                    }
                }
            }
            var sortedDict = from entry in tagStatistics orderby entry.Value descending select entry;
            return sortedDict.ToList();
        }
    }
}
