namespace Core
{
    public enum EventStatus
    { 
        Unread,
        Read,
        Processing,
        Completed,
    }
    public class BaseEvent
    {
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset DateCreated { get; set; } = DateTime.UtcNow;
        public DateTimeOffset DateModified { get; set; } = DateTime.UtcNow;
        public DateTimeOffset? DateClosed { get; set; }
        public DateTimeOffset? StartedWorkDateTime { get; set; }
        public DateTimeOffset? CompletedWorkDateTime { get; set; }
        public int Priority { get; set; }
        public string? Description { get; set; }
        public string Class { get; set; } = string.Empty;
        public string Subclass { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool Persist { get; set; }
        public EventStatus EventStatus { get; set; } = EventStatus.Unread;
        public Uri? Url { get; set; }
        public string? Payload { get; set; }
    }
}
