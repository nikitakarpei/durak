namespace EventStore;

public record RecordedEvent(Guid EventId, int EventNumber, string Type, string JsonData);
