using System.Runtime.CompilerServices;
using EventStore.MongoDb.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace EventStore.MongoDb;

public sealed class MongoDbEventStore : IEventStore
{
    private readonly MongoDbEventStoreOptions _options;
    private readonly IMongoCollection<StreamDocument> _streamCollection;

    public MongoDbEventStore(IMongoDatabase database, IOptions<MongoDbEventStoreOptions> options)
    {
        _options = options.Value;
        _streamCollection = database.GetCollection<StreamDocument>(_options.CollectionName);
    }

    public async Task AppendToStreamAsync(
        string streamId,
        IEnumerable<EventData> events,
        CancellationToken cancellationToken
    )
    {
        var eventsList = events.ToList();
        if (eventsList.Count == 0)
        {
            return;
        }

        var storedEvents = eventsList
            .Select(evt => new StoredEvent
            {
                EventId = evt.EventId,
                Type = evt.Type,
                Data = BsonDocument.Parse(evt.JsonData),
            })
            .ToList();

        var now = DateTime.UtcNow;
        var filter = Builders<StreamDocument>.Filter.Eq(s => s.StreamId, streamId);
        var update = Builders<StreamDocument>
            .Update.PushEach(s => s.Events, storedEvents)
            .SetOnInsert(s => s.Metadata.CreatedAt, now)
            .Set(s => s.Metadata.UpdatedAt, now)
            .Inc(s => s.Metadata.StreamPosition, eventsList.Count);

        await _streamCollection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken
        );
    }

    public async Task<IList<RecordedEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken)
    {
        var streamDoc = await _streamCollection
            .Find(Builders<StreamDocument>.Filter.Eq(s => s.StreamId, streamId))
            .FirstOrDefaultAsync(cancellationToken);

        if (streamDoc is null)
        {
            return [];
        }

        return streamDoc.Events.Select(evt => new RecordedEvent(evt.EventId, evt.Type, evt.Data.ToJson())).ToList();
    }

    public async IAsyncEnumerable<RecordedEvent> SubscribeToStreamAsync(
        string streamId,
        Guid? anchor,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<StreamDocument>>().Match(change =>
            (
                change.OperationType == ChangeStreamOperationType.Insert
                || change.OperationType == ChangeStreamOperationType.Update
            )
            && change.DocumentKey["_id"].AsString == streamId
        );

        using var cursor = await _streamCollection.WatchAsync(pipeline, cancellationToken: cancellationToken);

        Guid? lastProcessedEventId = anchor;

        if (anchor.HasValue)
        {
            var streamDoc = await _streamCollection
                .Find(Builders<StreamDocument>.Filter.Eq(s => s.StreamId, streamId))
                .FirstOrDefaultAsync(cancellationToken);

            if (streamDoc is not null)
            {
                var eventsToYield = streamDoc.Events.SkipWhile(e => e.EventId != anchor.Value).Skip(1);

                foreach (var evt in eventsToYield)
                {
                    yield return new RecordedEvent(evt.EventId, evt.Type, evt.Data.ToJson());
                    lastProcessedEventId = evt.EventId;
                }
            }
        }

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var change in cursor.Current)
            {
                IEnumerable<StoredEvent> allEvents;

                if (change.FullDocument is not null)
                {
                    allEvents = change.FullDocument.Events;
                }
                else
                {
                    allEvents = change
                        .UpdateDescription.UpdatedFields.Where(field => field.Name.StartsWith("Events."))
                        .OrderBy(field => int.Parse(field.Name.Split('.')[1]))
                        .Select(field => BsonSerializer.Deserialize<StoredEvent>(field.Value.AsBsonDocument))
                        .ToList();
                }

                IEnumerable<StoredEvent> eventsToYield;

                if (lastProcessedEventId.HasValue && allEvents.Any(e => e.EventId == lastProcessedEventId.Value))
                {
                    // lastProcessedEventId is in this batch - skip up to and including it
                    eventsToYield = allEvents.SkipWhile(e => e.EventId != lastProcessedEventId.Value).Skip(1);
                }
                else
                {
                    // Either no anchor, or we're already past it - yield all events
                    eventsToYield = allEvents;
                }

                foreach (var evt in eventsToYield)
                {
                    yield return new RecordedEvent(evt.EventId, evt.Type, evt.Data.ToJson());
                    lastProcessedEventId = evt.EventId;
                }
            }
        }
    }
}
