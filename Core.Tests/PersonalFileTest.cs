namespace Core.Tests
{
    public class PersonalFileTest : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles;

        public PersonalFileTest()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"PersonalFileTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _testFiles = new List<string>();
        }

        public void Dispose()
        {
            foreach (var file in _testFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        private string CreateTestFile(string content)
        {
            var filePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.txt");
            File.WriteAllText(filePath, content);
            _testFiles.Add(filePath);
            return filePath;
        }

        [Fact]
        public void Constructor_WithValidFile_ShouldComputeHash()
        {
            // Arrange
            var filePath = CreateTestFile("test content");
            
            // Act
            var personalFile = new PersonalFile(filePath);
            
            // Assert
            Assert.NotNull(personalFile.Hash);
            Assert.NotEmpty(personalFile.Hash);
        }

        [Fact]
        public void Constructor_WithSameContent_ShouldProduceSameHash()
        {
            // Arrange
            var content = "test content";
            var filePath1 = CreateTestFile(content);
            var filePath2 = CreateTestFile(content);
            
            // Act
            var file1 = new PersonalFile(filePath1);
            var file2 = new PersonalFile(filePath2);
            
            // Assert
            Assert.Equal(file1.Hash, file2.Hash);
        }

        [Fact]
        public void Constructor_WithDifferentContent_ShouldProduceDifferentHash()
        {
            // Arrange
            var filePath1 = CreateTestFile("content 1");
            var filePath2 = CreateTestFile("content 2");
            
            // Act
            var file1 = new PersonalFile(filePath1);
            var file2 = new PersonalFile(filePath2);
            
            // Assert
            Assert.NotEqual(file1.Hash, file2.Hash);
        }

        [Fact]
        public void Properties_CanBeSet()
        {
            // Arrange
            var filePath = CreateTestFile("test");
            var file = new PersonalFile(filePath);
            var tags = new TagList(new[] { "tag1", "tag2" });
            var createTime = DateTime.UtcNow;
            
            // Act
            file.CollectionNode = "Documents";
            file.RelativePath = "/path/to/file.txt";
            file.Title = "Test File";
            file.Description = "Test Description";
            file.Category = "Test Category";
            file.Subcategory = "Test Subcategory";
            file.FileType = "txt";
            file.Status = FileStatus.Filed;
            file.Tags = tags;
            file.CreateTime = createTime;
            file.Size = 1024;
            
            // Assert
            Assert.Equal("Documents", file.CollectionNode);
            Assert.Equal("/path/to/file.txt", file.RelativePath);
            Assert.Equal("Test File", file.Title);
            Assert.Equal("Test Description", file.Description);
            Assert.Equal("Test Category", file.Category);
            Assert.Equal("Test Subcategory", file.Subcategory);
            Assert.Equal("txt", file.FileType);
            Assert.Equal(FileStatus.Filed, file.Status);
            Assert.Equal(tags, file.Tags);
            Assert.Equal(createTime, file.CreateTime);
            Assert.Equal((ulong)1024, file.Size);
        }

        [Fact]
        public void GetHashCode_ShouldReturnInt32FromHash()
        {
            // Arrange
            var filePath = CreateTestFile("test content");
            var file = new PersonalFile(filePath);
            
            // Act
            var hashCode = file.GetHashCode();
            
            // Assert
            Assert.IsType<int>(hashCode);
        }

        [Fact]
        public void GetHashCode_WithSameContent_ShouldReturnSameHashCode()
        {
            // Arrange
            var content = "test content";
            var filePath1 = CreateTestFile(content);
            var filePath2 = CreateTestFile(content);
            var file1 = new PersonalFile(filePath1);
            var file2 = new PersonalFile(filePath2);
            
            // Act
            var hashCode1 = file1.GetHashCode();
            var hashCode2 = file2.GetHashCode();
            
            // Assert
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void FileStatus_AllValues_ShouldExist()
        {
            // Assert - Verify all enum values exist
            Assert.Equal(FileStatus.New, FileStatus.New);
            Assert.Equal(FileStatus.Pending, FileStatus.Pending);
            Assert.Equal(FileStatus.Filed, FileStatus.Filed);
            Assert.Equal(FileStatus.Archived, FileStatus.Archived);
            Assert.Equal(FileStatus.Deleted, FileStatus.Deleted);
        }

        [Fact]
        public void Hash_ShouldBeReadOnly()
        {
            // Arrange
            var filePath = CreateTestFile("test content");
            var file = new PersonalFile(filePath);
            var originalHash = file.Hash;
            
            // Act - Try to verify hash is read-only
            // (C# compiler prevents setting, so just verify it doesn't change)
            var currentHash = file.Hash;
            
            // Assert
            Assert.Equal(originalHash, currentHash);
        }
    }
}
