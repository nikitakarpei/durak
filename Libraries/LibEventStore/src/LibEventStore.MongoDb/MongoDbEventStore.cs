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

        return streamDoc
            .Events.Select((evt, index) => new RecordedEvent(evt.EventId, index, evt.Type, evt.Data.ToJson()))
            .ToList();
    }

    public async IAsyncEnumerable<RecordedEvent> SubscribeToStreamAsync(
        string streamId,
        int? fromEventNumber,
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

        int? lastProcessedEventNumber = fromEventNumber.HasValue ? fromEventNumber.Value - 1 : null;

        if (fromEventNumber.HasValue)
        {
            var streamDoc = await _streamCollection
                .Find(Builders<StreamDocument>.Filter.Eq(s => s.StreamId, streamId))
                .FirstOrDefaultAsync(cancellationToken);

            if (streamDoc is not null)
            {
                var eventsToYield = streamDoc.Events.Skip(fromEventNumber.Value);
                int currentEventNumber = fromEventNumber.Value;

                foreach (var evt in eventsToYield)
                {
                    yield return new RecordedEvent(evt.EventId, currentEventNumber, evt.Type, evt.Data.ToJson());
                    lastProcessedEventNumber = currentEventNumber;
                    currentEventNumber++;
                }
            }
        }

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var change in cursor.Current)
            {
                List<(StoredEvent evt, int index)> allEventsWithIndices;

                if (change.FullDocument is not null)
                {
                    allEventsWithIndices = change.FullDocument.Events.Select((evt, idx) => (evt, idx)).ToList();
                }
                else
                {
                    allEventsWithIndices = change
                        .UpdateDescription.UpdatedFields.Where(field => field.Name.StartsWith("Events."))
                        .Select(field =>
                        {
                            var index = int.Parse(field.Name.Split('.')[1]);
                            var evt = BsonSerializer.Deserialize<StoredEvent>(field.Value.AsBsonDocument);
                            return (evt, index);
                        })
                        .OrderBy(x => x.index)
                        .ToList();
                }

                IEnumerable<(StoredEvent evt, int index)> eventsToYield;

                if (lastProcessedEventNumber.HasValue)
                {
                    eventsToYield = allEventsWithIndices.Where(x => x.index > lastProcessedEventNumber.Value);
                }
                else
                {
                    eventsToYield = allEventsWithIndices;
                }

                foreach (var (evt, index) in eventsToYield)
                {
                    yield return new RecordedEvent(evt.EventId, index, evt.Type, evt.Data.ToJson());
                    lastProcessedEventNumber = index;
                }
            }
        }
    }
}
