using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EventStore.MongoDb.Models;

internal class EventDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string StreamName { get; set; } = string.Empty;

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid EventId { get; set; }

    public string Type { get; set; } = string.Empty;

    public BsonDocument Data { get; set; } = [];
}
