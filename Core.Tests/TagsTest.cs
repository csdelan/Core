namespace Core.Tests
{
    public class TagsTest
    {
        const string Tag1 = "tag1";
        const string Tag2 = "tag2";
        const string Tag3 = "tag3";

        [Fact]
        public void EmptyTest()
        {
            var tags = new TagList();
            Assert.True(tags.Count == 0);
        }

        [Fact]
        public void AddTagTest()
        {
            var tags = new TagList();
            tags.Add("tag1");
            tags.Add("tag2");
            Assert.True(tags.Count == 2);
            Assert.Contains("tag1",tags);
            Assert.Contains("tag2",tags);
            Assert.DoesNotContain("tag3",tags);
        }

        [Fact]
        public void AddRemoveTagTest()
        {
            var tags = new TagList();
            tags.Add("tag1");
            tags.Add("tag2");
            tags.Remove("tag1");
            Assert.True(tags.Count == 1);
            Assert.DoesNotContain("tag1",tags);
            Assert.Contains("tag2",tags);
            Assert.DoesNotContain("tag3", tags);
        }

        [Fact]
        public void AddRedundantTagTest()
        {
            var tags = new TagList();
            tags.Add("tag1");
            tags.Add("tag1");
            Assert.True(tags.Count == 1);
            Assert.Contains("tag1",tags);
            Assert.DoesNotContain("tag2",tags);
            Assert.DoesNotContain("tag3",tags);
        }

        [Fact]
        public void RemoveRedundantTagTest()
        {
            var tags = new TagList();
            tags.Add("tag1");
            tags.Remove("tag2");
            Assert.True(tags.Count == 1);
            Assert.Contains("tag1",tags);
            Assert.DoesNotContain("tag2",tags);
            Assert.DoesNotContain("tag3",tags);
            tags.Remove("tag1");
            Assert.True(tags.Count == 0);
            tags.Remove("tag1");
            Assert.True(tags.Count == 0);
        }
    }
}
