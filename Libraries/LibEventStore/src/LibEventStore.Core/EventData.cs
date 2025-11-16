namespace EventStore;

public record EventData(Guid EventId, string Type, string JsonData);
