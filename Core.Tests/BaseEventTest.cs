namespace Core.Tests
{
    public class BaseEventTest
    {
        [Fact]
        public void BaseEvent_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var evt = new BaseEvent();
            
            // Assert
            Assert.Equal(string.Empty, evt.Name);
            Assert.Equal(string.Empty, evt.Class);
            Assert.Equal(string.Empty, evt.Subclass);
            Assert.Equal(string.Empty, evt.Context);
            Assert.Equal(string.Empty, evt.Value);
            Assert.Equal(string.Empty, evt.Body);
            Assert.Equal(EventStatus.Unread, evt.EventStatus);
            Assert.Equal(0, evt.Priority);
            Assert.False(evt.Persist);
            Assert.Null(evt.DateClosed);
            Assert.Null(evt.StartedWorkDateTime);
            Assert.Null(evt.CompletedWorkDateTime);
            Assert.Null(evt.Description);
            Assert.Null(evt.Url);
            Assert.Null(evt.Payload);
        }

        [Fact]
        public void BaseEvent_DateCreated_ShouldBeSetAutomatically()
        {
            // Arrange
            var before = DateTimeOffset.UtcNow;
            
            // Act
            var evt = new BaseEvent();
            var after = DateTimeOffset.UtcNow;
            
            // Assert
            Assert.True(evt.DateCreated >= before && evt.DateCreated <= after);
        }

        [Fact]
        public void BaseEvent_DateModified_ShouldBeSetAutomatically()
        {
            // Arrange
            var before = DateTimeOffset.UtcNow;
            
            // Act
            var evt = new BaseEvent();
            var after = DateTimeOffset.UtcNow;
            
            // Assert
            Assert.True(evt.DateModified >= before && evt.DateModified <= after);
        }

        [Fact]
        public void BaseEvent_Properties_CanBeSet()
        {
            // Arrange
            var evt = new BaseEvent();
            var testUrl = new Uri("https://example.com");
            var testDate = DateTimeOffset.UtcNow;
            
            // Act
            evt.Name = "TestEvent";
            evt.Description = "Test description";
            evt.Class = "TestClass";
            evt.Subclass = "TestSubclass";
            evt.Context = "TestContext";
            evt.Value = "TestValue";
            evt.Body = "TestBody";
            evt.Priority = 5;
            evt.Persist = true;
            evt.EventStatus = EventStatus.Processing;
            evt.Url = testUrl;
            evt.Payload = "test payload";
            evt.DateClosed = testDate;
            evt.StartedWorkDateTime = testDate;
            evt.CompletedWorkDateTime = testDate;
            
            // Assert
            Assert.Equal("TestEvent", evt.Name);
            Assert.Equal("Test description", evt.Description);
            Assert.Equal("TestClass", evt.Class);
            Assert.Equal("TestSubclass", evt.Subclass);
            Assert.Equal("TestContext", evt.Context);
            Assert.Equal("TestValue", evt.Value);
            Assert.Equal("TestBody", evt.Body);
            Assert.Equal(5, evt.Priority);
            Assert.True(evt.Persist);
            Assert.Equal(EventStatus.Processing, evt.EventStatus);
            Assert.Equal(testUrl, evt.Url);
            Assert.Equal("test payload", evt.Payload);
            Assert.Equal(testDate, evt.DateClosed);
            Assert.Equal(testDate, evt.StartedWorkDateTime);
            Assert.Equal(testDate, evt.CompletedWorkDateTime);
        }

        [Fact]
        public void EventStatus_AllValues_ShouldExist()
        {
            // Assert - Verify all enum values exist
            Assert.Equal(EventStatus.Unread, EventStatus.Unread);
            Assert.Equal(EventStatus.Read, EventStatus.Read);
            Assert.Equal(EventStatus.Processing, EventStatus.Processing);
            Assert.Equal(EventStatus.Completed, EventStatus.Completed);
        }
    }
}
