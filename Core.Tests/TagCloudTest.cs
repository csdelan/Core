using System.Net.NetworkInformation;

namespace Core.Tests
{
    public class TagCloudTest
    {
        const string Tag1 = "tag1";
        const string Tag2 = "tag2";
        const string Tag3 = "tag3";

        public class Taggable : ITaggable
        {
            public required TagList Tags { get; set; }
        }

        [Fact]
        public void GetAllTagsTest()
        {
            var obj1 = new Taggable { Tags = new TagList(new string[] { Tag1, Tag2 }) };
            var obj2 = new Taggable { Tags = new TagList(new string[] { Tag2, Tag3 }) };
            var objectList = new List<ITaggable> { obj1, obj2 };
            var tagCloud = new TagCloud(objectList);
            var tags = tagCloud.GetAllTags();
            Assert.True(tags.Count == 3);
            Assert.Contains(Tag1, tags);
            Assert.Contains(Tag2, tags);
            Assert.Contains(Tag3, tags);
        }

        [Fact]
        public void GetNoTagsTest()
        {
            var objectList = new List<ITaggable> { };
            var tagCloud = new TagCloud(objectList);
            var tags = tagCloud.GetAllTags();
            Assert.True(tags.Count == 0);
            Assert.DoesNotContain(Tag1, tags);
            Assert.DoesNotContain(Tag2, tags);
            Assert.DoesNotContain(Tag3, tags);

            var obj1 = new Taggable { Tags = new TagList(new string[] { }) };
            objectList = new List<ITaggable> { obj1 };
            tagCloud = new TagCloud(objectList);
            tags = tagCloud.GetAllTags();
            Assert.True(tags.Count == 0);
            Assert.DoesNotContain(Tag1, tags);
            Assert.DoesNotContain(Tag2, tags);
            Assert.DoesNotContain(Tag3, tags);
        }

        [Fact]
        public void GetNullTagsTest()
        {
            Assert.Throws<ArgumentNullException>(() => new TagCloud(null));
        }

        [Fact]
        public void EmptyTagStatisticsTest()
        {
            var objectList = new List<ITaggable> { };
            var tagCloud = new TagCloud(objectList);
            var tagStatistics = tagCloud.GetTagStatistics();
            Assert.True(tagStatistics.Count == 0);

            var obj1 = new Taggable { Tags = new TagList(new string[] { }) };
            objectList = new List<ITaggable> { obj1 };
            tagCloud = new TagCloud(objectList);
            tagStatistics = tagCloud.GetTagStatistics();
            Assert.True(tagStatistics.Count == 0);
        }

        [Fact]
        public void CountTagStatisticsTest()
        {
            var obj1 = new Taggable { Tags = new TagList(new string[] { Tag1, Tag2 }) };
            var obj2 = new Taggable { Tags = new TagList(new string[] { Tag2, Tag3 }) };
            var objectList = new List<ITaggable> { obj1, obj2 };
            var tagCloud = new TagCloud(objectList);
            var tagStatistics = tagCloud.GetTagStatistics();
            Assert.True(tagStatistics.Count == 3);
            Assert.Contains(new KeyValuePair<string, int>(Tag1, 1), tagStatistics);
            Assert.Contains(new KeyValuePair<string, int>(Tag2, 2), tagStatistics);
            Assert.Contains(new KeyValuePair<string, int>(Tag3, 1), tagStatistics);
        }
    }
}
