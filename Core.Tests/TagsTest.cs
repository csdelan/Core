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
            tags.Add(Tag1);
            tags.Add(Tag2);
            Assert.True(tags.Count == 2);
            Assert.Contains(Tag1,tags);
            Assert.Contains(Tag2,tags);
            Assert.DoesNotContain(Tag3,tags);
        }

        [Fact]
        public void AddRemoveTagTest()
        {
            var tags = new TagList();
            tags.Add(Tag1);
            tags.Add(Tag2);
            tags.Remove(Tag1);
            Assert.True(tags.Count == 1);
            Assert.DoesNotContain(Tag1,tags);
            Assert.Contains(Tag2,tags);
            Assert.DoesNotContain(Tag3, tags);
        }

        [Fact]
        public void AddRedundantTagTest()
        {
            var tags = new TagList();
            tags.Add(Tag1);
            tags.Add(Tag1);
            Assert.True(tags.Count == 1);
            Assert.Contains(Tag1,tags);
            Assert.DoesNotContain(Tag2,tags);
            Assert.DoesNotContain(Tag3,tags);
        }

        [Fact]
        public void RemoveRedundantTagTest()
        {
            var tags = new TagList();
            tags.Add(Tag1);
            tags.Remove(Tag2);
            Assert.True(tags.Count == 1);
            Assert.Contains(Tag1,tags);
            Assert.DoesNotContain(Tag2,tags);
            Assert.DoesNotContain(Tag3,tags);
            tags.Remove(Tag1);
            Assert.True(tags.Count == 0);
            tags.Remove(Tag1);
            Assert.True(tags.Count == 0);
        }

        [Fact]
        public void TagToStringTest()
        {
            var tags = new TagList();
            Assert.True(tags.ToString() == string.Empty);

            tags.Add(Tag3);
            tags.Add(Tag1);
            tags.Add(Tag2);
            Assert.True(tags.Count == 3);
            Assert.True(tags.ToString() == "tag3 tag1 tag2");
        }

        [Fact]
        public void StringArrayConstructor_ShouldInitializeTags()
        {
            // Arrange & Act
            var tags = new TagList(new string[] { Tag1, Tag2, Tag3 });
            
            // Assert
            Assert.Equal(3, tags.Count);
            Assert.Contains(Tag1, tags);
            Assert.Contains(Tag2, tags);
            Assert.Contains(Tag3, tags);
        }

        [Fact]
        public void StringArrayConstructor_WithDuplicates_ShouldRemoveDuplicates()
        {
            // Arrange & Act
            var tags = new TagList(new string[] { Tag1, Tag2, Tag1, Tag3 });
            
            // Assert
            Assert.Equal(3, tags.Count);
            Assert.Contains(Tag1, tags);
            Assert.Contains(Tag2, tags);
            Assert.Contains(Tag3, tags);
        }

        [Fact]
        public void StringConstructor_WithSpaceSeparatedTags_ShouldParseTags()
        {
            // Arrange & Act
            var tags = new TagList("tag1 tag2 tag3");
            
            // Assert
            Assert.Equal(3, tags.Count);
            Assert.Contains("tag1", tags);
            Assert.Contains("tag2", tags);
            Assert.Contains("tag3", tags);
        }

        [Fact]
        public void StringConstructor_WithExtraSpaces_ShouldTrimAndParse()
        {
            // Arrange & Act
            var tags = new TagList("  tag1   tag2    tag3  ");
            
            // Assert
            Assert.Equal(3, tags.Count);
            Assert.Contains("tag1", tags);
            Assert.Contains("tag2", tags);
            Assert.Contains("tag3", tags);
        }

        [Fact]
        public void StringConstructor_WithNullString_ShouldCreateEmptySet()
        {
            // Arrange & Act
            var tags = new TagList((string?)null);
            
            // Assert
            Assert.Equal(0, tags.Count);
        }

        [Fact]
        public void StringConstructor_WithEmptyString_ShouldCreateEmptySet()
        {
            // Arrange & Act
            var tags = new TagList(string.Empty);
            
            // Assert
            Assert.Equal(0, tags.Count);
        }

        [Fact]
        public void StringConstructor_WithWhitespaceString_ShouldCreateEmptySet()
        {
            // Arrange & Act
            var tags = new TagList("   ");
            
            // Assert
            Assert.Equal(0, tags.Count);
        }
    }
}
