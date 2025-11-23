namespace Core.Tests
{
    public class DateTimeOffsetExtensionsTest
    {
        [Fact]
        public void WithDay_ShouldReturnDateWithNewDay()
        {
            // Arrange
            var original = new DateTimeOffset(2024, 5, 15, 10, 30, 45, TimeSpan.FromHours(2));
            
            // Act
            var result = original.WithDay(20);
            
            // Assert
            Assert.Equal(2024, result.Year);
            Assert.Equal(5, result.Month);
            Assert.Equal(20, result.Day);
            Assert.Equal(10, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);
            Assert.Equal(TimeSpan.FromHours(2), result.Offset);
        }

        [Fact]
        public void WithDayAndMonth_ShouldReturnDateWithNewMonthAndDay()
        {
            // Arrange
            var original = new DateTimeOffset(2024, 5, 15, 10, 30, 45, TimeSpan.FromHours(-5));
            
            // Act
            var result = original.WithDayAndMonth(12, 25);
            
            // Assert
            Assert.Equal(2024, result.Year);
            Assert.Equal(12, result.Month);
            Assert.Equal(25, result.Day);
            Assert.Equal(10, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);
            Assert.Equal(TimeSpan.FromHours(-5), result.Offset);
        }

        [Fact]
        public void TruncateToMinute_ShouldRemoveSecondsAndMilliseconds()
        {
            // Arrange
            var original = new DateTimeOffset(2024, 5, 15, 10, 30, 45, 123, TimeSpan.FromHours(0));
            
            // Act
            var result = original.TruncateToMinute();
            
            // Assert
            Assert.Equal(2024, result.Year);
            Assert.Equal(5, result.Month);
            Assert.Equal(15, result.Day);
            Assert.Equal(10, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(0, result.Second);
            Assert.Equal(0, result.Millisecond);
            Assert.Equal(TimeSpan.FromHours(0), result.Offset);
        }

        [Fact]
        public void TruncateToMinute_ShouldHandleUtcTime()
        {
            // Arrange
            var original = DateTimeOffset.UtcNow;
            
            // Act
            var result = original.TruncateToMinute();
            
            // Assert
            Assert.Equal(0, result.Second);
            Assert.Equal(0, result.Millisecond);
            Assert.Equal(original.Offset, result.Offset);
        }
    }
}
