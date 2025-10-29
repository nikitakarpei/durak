using System.Text.Json;

namespace EventStore;

public abstract record RecordedEvent(Guid EventId, string Type, string JsonData);

public record ExistingRecordedEvent(Guid EventId, string Type, string JsonData, bool IsLast)
    : RecordedEvent(EventId, Type, JsonData);

public record NewRecordedEvent(Guid EventId, string Type, string JsonData) : RecordedEvent(EventId, Type, JsonData);
