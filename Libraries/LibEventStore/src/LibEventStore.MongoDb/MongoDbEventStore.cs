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
    private const int MongoDbNamespaceExistsErrorCode = 48;

    public MongoDbEventStore(IMongoDatabase database, IOptions<MongoDbEventStoreOptions> options)
    {
        _database = database;
        _options = options.Value;
    }

    public async Task AppendToStreamAsync(
        string streamName,
        IEnumerable<EventData> events,
        CancellationToken cancellationToken
    )
    {
        _eventCollection ??= await GetEventCollectionAsync(cancellationToken);

        var documents = events
            .Select(evt => new EventDocument
            {
                Id = ObjectId.GenerateNewId(),
                StreamName = streamName,
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

    public async Task<StreamSubscription> SubscribeToStreamAsync(string streamName, CancellationToken cancellationToken)
    {
        _eventCollection ??= await GetEventCollectionAsync(cancellationToken);

        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<EventDocument>>().Match(change =>
            change.OperationType == ChangeStreamOperationType.Insert && change.FullDocument.StreamName == streamName
        );

        var cursor = await _eventCollection.WatchAsync(pipeline, null, cancellationToken);

        var existingEvents = await _eventCollection
            .Find(Builders<EventDocument>.Filter.Eq(e => e.StreamName, streamName))
            .SortBy(e => e.Id)
            .ToListAsync(cancellationToken);

        async IAsyncEnumerable<RecordedEvent> EventsFactory([EnumeratorCancellation] CancellationToken ct)
        {
            // Yield existing events first
            for (int i = 0; i < existingEvents.Count; i++)
            {
                var doc = existingEvents[i];
                bool isLast = i == existingEvents.Count - 1;
                yield return new ExistingRecordedEvent(doc.EventId, doc.Type, doc.Data.ToJson(), isLast);
            }

            ObjectId? lastExistingId = existingEvents.LastOrDefault()?.Id;

            // Then watch for new events
            while (await cursor.MoveNextAsync(ct))
            {
                foreach (var change in cursor.Current)
                {
                    if (change.FullDocument is null)
                    {
                        continue;
                    }
                    if (lastExistingId.HasValue && change.FullDocument.Id.CompareTo(lastExistingId.Value) <= 0)
                    {
                        continue;
                    }

                    yield return new NewRecordedEvent(
                        change.FullDocument.EventId,
                        change.FullDocument.Type,
                        change.FullDocument.Data.ToJson()
                    );
                }
            }
        }

        // Dispose action to clean up the cursor
        ValueTask DisposeAction()
        {
            cursor.Dispose();
            return ValueTask.CompletedTask;
        }

        return new StreamSubscription(EventsFactory, DisposeAction);
    }

    private async Task<IMongoCollection<EventDocument>> GetEventCollectionAsync(CancellationToken cancellationToken)
    {
        bool created = false;

        try
        {
            await _database.CreateCollectionAsync(_options.CollectionName, cancellationToken: cancellationToken);
            created = true;
        }
        catch (MongoCommandException ex) when (ex.Code == MongoDbNamespaceExistsErrorCode)
        {
            // Collection already exists
        }

        var collection = _database.GetCollection<EventDocument>(_options.CollectionName);

        if (created)
        {
            await collection.Indexes.CreateOneAsync(
                new CreateIndexModel<EventDocument>(Builders<EventDocument>.IndexKeys.Hashed(e => e.StreamName)),
                null,
                cancellationToken
            );
        }

        return collection;
    }
}
