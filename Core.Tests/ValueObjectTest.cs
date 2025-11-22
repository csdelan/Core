namespace Core.Tests
{
    // Test implementation of ValueObject for testing purposes
    public class TestValueObject : ValueObject
    {
        public string Name { get; }
        public int Value { get; }

        public TestValueObject(string name, int value)
        {
            Name = name;
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }

    public class TestValueObjectSingleProperty : ValueObject
    {
        public string Property { get; }

        public TestValueObjectSingleProperty(string property)
        {
            Property = property;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Property;
        }
    }

    public class ValueObjectTest
    {
        [Fact]
        public void Equals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 42);
            
            // Act & Assert
            Assert.True(obj1.Equals(obj2));
            Assert.True(obj2.Equals(obj1));
        }

        [Fact]
        public void Equals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 43);
            
            // Act & Assert
            Assert.False(obj1.Equals(obj2));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            
            // Act & Assert
            Assert.False(obj1.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObjectSingleProperty("test");
            
            // Act & Assert
            Assert.False(obj1.Equals(obj2));
        }

        [Fact]
        public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 42);
            
            // Act
            var hash1 = obj1.GetHashCode();
            var hash2 = obj2.GetHashCode();
            
            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 43);
            
            // Act
            var hash1 = obj1.GetHashCode();
            var hash2 = obj2.GetHashCode();
            
            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void GetHashCode_CalledMultipleTimes_ShouldReturnCachedValue()
        {
            // Arrange
            var obj = new TestValueObject("test", 42);
            
            // Act
            var hash1 = obj.GetHashCode();
            var hash2 = obj.GetHashCode();
            var hash3 = obj.GetHashCode();
            
            // Assert
            Assert.Equal(hash1, hash2);
            Assert.Equal(hash2, hash3);
        }

        [Fact]
        public void CompareTo_WithSameValues_ShouldReturnZero()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 42);
            
            // Act
            var result = obj1.CompareTo(obj2);
            
            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CompareTo_WithNull_ShouldReturnPositive()
        {
            // Arrange
            var obj = new TestValueObject("test", 42);
            
            // Act
            var result = obj.CompareTo(null);
            
            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_WithDifferentValues_ShouldReturnNonZero()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 43);
            
            // Act
            var result = obj1.CompareTo(obj2);
            
            // Assert
            Assert.NotEqual(0, result);
        }

        [Fact]
        public void CompareTo_WithDifferentStrings_ShouldCompareCorrectly()
        {
            // Arrange
            var obj1 = new TestValueObject("apple", 42);
            var obj2 = new TestValueObject("banana", 42);
            
            // Act
            var result = obj1.CompareTo(obj2);
            
            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void OperatorEquals_WithSameValues_ShouldReturnTrue()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 42);
            
            // Act & Assert
            Assert.True(obj1 == obj2);
        }

        [Fact]
        public void OperatorEquals_WithDifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 43);
            
            // Act & Assert
            Assert.False(obj1 == obj2);
        }

        [Fact]
        public void OperatorEquals_WithBothNull_ShouldReturnTrue()
        {
            // Arrange
            TestValueObject? obj1 = null;
            TestValueObject? obj2 = null;
            
            // Act & Assert
            Assert.True(obj1 == obj2);
        }

        [Fact]
        public void OperatorEquals_WithOneNull_ShouldReturnFalse()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            TestValueObject? obj2 = null;
            
            // Act & Assert
            Assert.False(obj1 == obj2);
            Assert.False(obj2 == obj1);
        }

        [Fact]
        public void OperatorNotEquals_WithSameValues_ShouldReturnFalse()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 42);
            
            // Act & Assert
            Assert.False(obj1 != obj2);
        }

        [Fact]
        public void OperatorNotEquals_WithDifferentValues_ShouldReturnTrue()
        {
            // Arrange
            var obj1 = new TestValueObject("test", 42);
            var obj2 = new TestValueObject("test", 43);
            
            // Act & Assert
            Assert.True(obj1 != obj2);
        }
    }
}
