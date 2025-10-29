using System.Text.Json;

namespace EventStore;

public record EventData(Guid EventId, string Type, string JsonData);
