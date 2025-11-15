using System.Runtime.CompilerServices;
using EventStore.MongoDb.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EventStore.MongoDb;

public sealed class MongoDbEventStore : IEventStore
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbEventStoreOptions _options;

    private IMongoCollection<EventDocument>? _eventCollection = null;

    public MongoDbEventStore(IMongoDatabase database, IOptions<MongoDbEventStoreOptions> options)
    {
        _database = database;
        _options = options.Value;
    }

    public async Task AppendToStreamAsync(
        string streamId,
        IEnumerable<EventData> events,
        CancellationToken cancellationToken
    )
    {
        _eventCollection ??= await GetEventCollectionAsync(cancellationToken);

        var documents = events
            .Select(evt => new EventDocument
            {
                Id = ObjectId.GenerateNewId(),
                StreamId = streamId,
                EventId = evt.EventId,
                Type = evt.Type,
                Data = BsonDocument.Parse(evt.JsonData),
            })
            .ToList();

        if (documents.Count > 0)
        {
            await _eventCollection.InsertManyAsync(documents, null, cancellationToken);
        }
    }

    public async Task<IList<RecordedEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken)
    {
        _eventCollection ??= await GetEventCollectionAsync(cancellationToken);

        var documents = await _eventCollection
            .Find(Builders<EventDocument>.Filter.Eq(e => e.StreamId, streamId))
            .SortBy(e => e.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(doc => new RecordedEvent(doc.EventId, doc.Type, doc.Data.ToJson())).ToList();
    }

    public async IAsyncEnumerable<RecordedEvent> SubscribeToStreamAsync(
        string streamId,
        Guid? anchor,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        _eventCollection ??= await GetEventCollectionAsync(cancellationToken);

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<EventDocument>>().Match(change =>
            change.OperationType == ChangeStreamOperationType.Insert && change.FullDocument.StreamId == streamId
        );

        using var cursor = await _eventCollection.WatchAsync(pipeline, null, cancellationToken);

        ObjectId? lastYieldedId = null;

        // If anchor is provided, yield existing events after the anchor
        if (anchor.HasValue)
        {
            var documents = await _eventCollection
                .Find(Builders<EventDocument>.Filter.Eq(e => e.StreamId, streamId))
                .SortBy(e => e.Id)
                .ToListAsync(cancellationToken);

            bool foundAnchor = false;
            foreach (var doc in documents)
            {
                if (foundAnchor)
                {
                    yield return new RecordedEvent(doc.EventId, doc.Type, doc.Data.ToJson());
                    lastYieldedId = doc.Id;
                }
                else if (doc.EventId == anchor.Value)
                {
                    foundAnchor = true;
                    lastYieldedId = doc.Id;
                }
            }
        }

        // Watch for new events
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var change in cursor.Current)
            {
                if (change.FullDocument is null)
                {
                    continue;
                }

                // Skip events we've already yielded
                if (lastYieldedId.HasValue && change.FullDocument.Id.CompareTo(lastYieldedId.Value) <= 0)
                {
                    continue;
                }

                yield return new RecordedEvent(
                    change.FullDocument.EventId,
                    change.FullDocument.Type,
                    change.FullDocument.Data.ToJson()
                );

                lastYieldedId = change.FullDocument.Id;
            }
        }
    }

    private async Task<IMongoCollection<EventDocument>> GetEventCollectionAsync(CancellationToken cancellationToken)
    {
        await _database.CreateCollectionAsync(_options.CollectionName, cancellationToken: cancellationToken);

        var collection = _database.GetCollection<EventDocument>(_options.CollectionName);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<EventDocument>(Builders<EventDocument>.IndexKeys.Hashed(e => e.StreamId)),
            null,
            cancellationToken
        );

        return collection;
    }
}
