namespace Core
{
    /// <summary>
    /// Represents a collection of objects that implement the <see cref="ITaggable"/> interface,  providing
    /// functionality to retrieve and analyze their associated tags.
    /// </summary>
    /// <remarks>The <see cref="TagCloud"/> class is designed to manage a collection of objects that expose
    /// tags  through the <see cref="ITaggable.Tags"/> property. It provides methods to retrieve all tags  and to
    /// generate tag statistics, such as occurrence counts. This class ensures that the  collection is initialized and
    /// cannot be null.</remarks>
    /// <param name="objectList"></param>
    public class TagCloud(List<ITaggable>? objectList)
    {
        private readonly List<ITaggable> _objectList = objectList ?? throw new ArgumentNullException(nameof(objectList));

        /// <summary>
        /// Retrieves a list of all tags from the objects in the collection.
        /// </summary>
        /// <remarks>This method aggregates tags from all objects in the collection that implement  the
        /// <see cref="ITaggable"/> interface. Each object's <see cref="ITaggable.Tags"/>  property is accessed to
        /// collect its tags.</remarks>
        /// <returns>A list of strings containing all tags associated with the objects.  The list may contain duplicate tags if
        /// multiple objects share the same tag,  and it will be empty if no tags are present.</returns>
        public List<string> GetAllTags()
        {
            TagList tags = [];
            foreach (ITaggable taggable in this._objectList)
            {
                foreach (string tag in taggable.Tags)
                {
                    tags.Add(tag);
                }
            }
            return [.. tags];
        }

        /// <summary>
        /// Retrieves a list of tags and their respective occurrence counts, sorted in descending order by count.
        /// </summary>
        /// <remarks>This method aggregates tag data from all objects in the internal collection that
        /// implement the <see cref="ITaggable"/> interface. Each tag is counted based on its occurrences across all
        /// objects.</remarks>
        /// <returns>A list of key-value pairs where the key is the tag name and the value is the number of times the tag
        /// appears. The list is sorted in descending order by the occurrence count.</returns>
        public List<KeyValuePair<string, int>> GetTagStatistics()
        {
            Dictionary<string, int> tagStatistics = new();
            foreach (ITaggable taggable in _objectList)
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
