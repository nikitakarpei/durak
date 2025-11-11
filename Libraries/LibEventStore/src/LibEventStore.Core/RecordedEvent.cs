namespace EventStore;

public record RecordedEvent(Guid EventId, string Type, string JsonData);
