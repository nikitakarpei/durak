using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EventStore.MongoDb.Models;

internal class StreamDocument
{
    [BsonId]
    public string StreamId { get; set; } = string.Empty;
    public StreamMetadata Metadata { get; set; } = new();
    public List<StoredEvent> Events { get; set; } = [];
}

internal class StreamMetadata
{
    public long StreamPosition { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

internal class StoredEvent
{
    public Guid EventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public BsonDocument Data { get; set; } = new();
}
