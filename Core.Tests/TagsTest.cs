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
            var tags = new Tags();
            Assert.True(tags.TagsCollection.Count == 0);
            Assert.True(tags.TagString.Trim() == string.Empty);
        }

        [Fact]
        public void AddTagTest()
        {
            var tags = new Tags();
            tags.AddTag("tag1");
            tags.AddTag("tag2");
            Assert.True(tags.TagsCollection.Count == 2);
            Assert.True(tags.ContainsTag("tag1"));
            Assert.True(tags.ContainsTag("tag2"));
            Assert.False(tags.ContainsTag("tag3"));
        }

        [Fact]
        public void AddRemoveTagTest()
        {
            var tags = new Tags();
            tags.AddTag("tag1");
            tags.AddTag("tag2");
            tags.RemoveTag("tag1");
            Assert.True(tags.TagsCollection.Count == 1);
            Assert.False(tags.ContainsTag("tag1"));
            Assert.True(tags.ContainsTag("tag2"));
            Assert.False(tags.ContainsTag("tag3"));
        }

        [Fact]
        public void AddRedundantTagTest()
        {
            var tags = new Tags();
            tags.AddTag("tag1");
            tags.AddTag("tag1");
            Assert.True(tags.TagsCollection.Count == 1);
            Assert.True(tags.ContainsTag("tag1"));
            Assert.False(tags.ContainsTag("tag2"));
            Assert.False(tags.ContainsTag("tag3"));
        }

        [Fact]
        public void RemoveRedundantTagTest()
        {
            var tags = new Tags();
            tags.AddTag("tag1");
            tags.RemoveTag("tag2");
            Assert.True(tags.TagsCollection.Count == 1);
            Assert.True(tags.ContainsTag("tag1"));
            Assert.False(tags.ContainsTag("tag2"));
            Assert.False(tags.ContainsTag("tag3"));
            tags.RemoveTag("tag1");
            Assert.True(tags.TagsCollection.Count == 0);
            tags.RemoveTag("tag1");
            Assert.True(tags.TagsCollection.Count == 0);
        }
    }
}
