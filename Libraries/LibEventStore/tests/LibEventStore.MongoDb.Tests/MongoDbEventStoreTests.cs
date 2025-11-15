using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace EventStore.MongoDb.Tests;

public class MongoDbEventStoreTests : IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;
    private IMongoDatabase? _database;
    private MongoDbEventStore? _eventStore;

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithReplicaSet("rs0") // Required for Change Streams
            .Build();

        await _mongoContainer.StartAsync();

        var client = new MongoClient(_mongoContainer.GetConnectionString());
        _database = client.GetDatabase("test_eventstore");
        var options = new MongoDbEventStoreOptions { CollectionName = "events" };
        _eventStore = new MongoDbEventStore(_database, Options.Create(options));
    }

    public async Task DisposeAsync()
    {
        if (_mongoContainer != null)
        {
            await _mongoContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task AppendToStreamAsync_EmptyEvents_ShouldNotThrow()
    {
        // Arrange
        var streamName = "test-stream";
        var events = Array.Empty<EventData>();

        // Act
        var act = async () => await _eventStore!.AppendToStreamAsync(streamName, events, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AppendToStreamAsync_SingleEvent_ShouldStore()
    {
        // Arrange
        var streamName = "test-stream";
        var eventId = Guid.NewGuid();
        var eventData = new EventData(eventId, "TestEvent", JsonSerializer.Serialize(new { test = "data" }));

        // Act
        await _eventStore!.AppendToStreamAsync(streamName, new[] { eventData }, CancellationToken.None);

        // Assert - verify by reading
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var receivedEvents = await _eventStore.ReadStreamAsync(streamName, cts.Token);

        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].EventId.Should().Be(eventId);
        receivedEvents[0].Type.Should().Be(eventData.Type);

        var expectedJson = JsonSerializer.Deserialize<JsonElement>(eventData.JsonData);
        var actualJson = JsonSerializer.Deserialize<JsonElement>(receivedEvents[0].JsonData);
        JsonElement.DeepEquals(actualJson, expectedJson).Should().BeTrue();
    }

    [Fact]
    public async Task AppendToStreamAsync_MultipleEvents_ShouldPreserveOrder()
    {
        // Arrange
        var streamName = "test-stream";
        var events = Enumerable
            .Range(1, 10)
            .Select(i => new EventData(
                Guid.NewGuid(),
                $"Event{i}",
                JsonSerializer.Serialize(new { test = $"data{i}" })
            ))
            .ToList();

        // Act
        await _eventStore!.AppendToStreamAsync(streamName, events, CancellationToken.None);

        // Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var receivedEvents = await _eventStore.ReadStreamAsync(streamName, cts.Token);

        receivedEvents.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            receivedEvents[i].Type.Should().Be(events[i].Type);

            var expectedJson = JsonSerializer.Deserialize<JsonElement>(events[i].JsonData);
            var actualJson = JsonSerializer.Deserialize<JsonElement>(receivedEvents[i].JsonData);
            JsonElement.DeepEquals(actualJson, expectedJson).Should().BeTrue();
        }
    }

    [Fact]
    public async Task SubscribeToStreamAsync_EmptyStream_ShouldWaitForNewEvents()
    {
        // Arrange
        var streamName = "test-stream";
        var eventId = Guid.NewGuid();

        // Act - subscribe first
        var subscribedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var subscribeTask = Task.Run(async () =>
        {
            await foreach (var evt in _eventStore!.SubscribeToStreamAsync(streamName, null, cts.Token))
            {
                subscribedEvents.Add(evt);
                if (subscribedEvents.Count >= 1)
                {
                    break;
                }
            }
        });

        // Give MongoDB change stream a moment to set up
        await Task.Delay(1000);

        // Append event
        await _eventStore!.AppendToStreamAsync(
            streamName,
            new[] { new EventData(eventId, "NewEvent", JsonSerializer.Serialize(new { test = "data" })) },
            CancellationToken.None
        );

        await subscribeTask;

        // Assert
        subscribedEvents.Should().HaveCount(1);
        subscribedEvents[0].EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task SubscribeToStreamAsync_WithExistingEvents_ShouldEmitExistingThenNew()
    {
        // Arrange
        var streamName = "test-stream";
        var existingEvent = new EventData(Guid.NewGuid(), "Existing", JsonSerializer.Serialize(new { test = "old" }));
        var newEvent = new EventData(Guid.NewGuid(), "New", JsonSerializer.Serialize(new { test = "new" }));

        await _eventStore!.AppendToStreamAsync(streamName, new[] { existingEvent }, CancellationToken.None);

        // Act
        var receivedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var subscribeTask = Task.Run(async () =>
        {
            // Subscribe with the anchor set to the existing event ID to get new events after it
            await foreach (var evt in _eventStore.SubscribeToStreamAsync(streamName, existingEvent.EventId, cts.Token))
            {
                receivedEvents.Add(evt);
                if (receivedEvents.Count >= 1)
                {
                    break;
                }
            }
        });

        // Give MongoDB change stream a moment to set up
        await Task.Delay(1000);

        // Append new event
        await _eventStore!.AppendToStreamAsync(streamName, new[] { newEvent }, CancellationToken.None);

        await subscribeTask;

        // Assert
        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].EventId.Should().Be(newEvent.EventId);
    }

    [Fact]
    public async Task ReadStreamAsync_MultipleReaders_ShouldAllReceiveEvents()
    {
        // Arrange
        var streamName = "test-stream";
        var events = Enumerable
            .Range(1, 5)
            .Select(i => new EventData(
                Guid.NewGuid(),
                $"Event{i}",
                JsonSerializer.Serialize(new { test = $"data{i}" })
            ))
            .ToList();

        await _eventStore!.AppendToStreamAsync(streamName, events, CancellationToken.None);

        // Act - multiple readers
        IList<RecordedEvent>? reader1Events = null;
        IList<RecordedEvent>? reader2Events = null;
        IList<RecordedEvent>? reader3Events = null;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var read1Task = Task.Run(async () =>
        {
            reader1Events = await _eventStore.ReadStreamAsync(streamName, cts.Token);
        });

        var read2Task = Task.Run(async () =>
        {
            reader2Events = await _eventStore.ReadStreamAsync(streamName, cts.Token);
        });

        var read3Task = Task.Run(async () =>
        {
            reader3Events = await _eventStore.ReadStreamAsync(streamName, cts.Token);
        });

        await Task.WhenAll(read1Task, read2Task, read3Task);

        // Assert
        reader1Events!.Should().HaveCount(5);
        reader2Events!.Should().HaveCount(5);
        reader3Events!.Should().HaveCount(5);

        var expectedIds = events.Select(e => e.EventId).ToList();
        reader1Events!.Select(e => e.EventId).Should().BeEquivalentTo(expectedIds);
        reader2Events!.Select(e => e.EventId).Should().BeEquivalentTo(expectedIds);
        reader3Events!.Select(e => e.EventId).Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task AppendToStreamAsync_ToMultipleStreams_ShouldIsolate()
    {
        // Arrange
        var stream1 = "stream-1";
        var stream2 = "stream-2";
        var event1 = new EventData(Guid.NewGuid(), "Event1", JsonSerializer.Serialize(new { test = "data1" }));
        var event2 = new EventData(Guid.NewGuid(), "Event2", JsonSerializer.Serialize(new { test = "data2" }));

        // Act
        await _eventStore!.AppendToStreamAsync(stream1, new[] { event1 }, CancellationToken.None);
        await _eventStore!.AppendToStreamAsync(stream2, new[] { event2 }, CancellationToken.None);

        // Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var stream1Events = await _eventStore.ReadStreamAsync(stream1, cts.Token);
        var stream2Events = await _eventStore.ReadStreamAsync(stream2, cts.Token);

        stream1Events.Should().HaveCount(1);
        stream1Events[0].EventId.Should().Be(event1.EventId);

        stream2Events.Should().HaveCount(1);
        stream2Events[0].EventId.Should().Be(event2.EventId);
    }

    [Fact]
    public async Task SubscribeToStreamAsync_CancellationToken_ShouldStopSubscription()
    {
        // Arrange
        var streamName = "test-stream";

        // Act
        var receivedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            await foreach (var evt in _eventStore!.SubscribeToStreamAsync(streamName, null, cts.Token))
            {
                receivedEvents.Add(evt);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [Fact]
    public async Task SubscribeToStreamAsync_WithAnchor_ShouldEmitEventsAfterAnchor()
    {
        // Arrange
        var streamName = "test-stream";
        var event1 = new EventData(Guid.NewGuid(), "Event1", JsonSerializer.Serialize(new { test = "data1" }));
        var event2 = new EventData(Guid.NewGuid(), "Event2", JsonSerializer.Serialize(new { test = "data2" }));
        var event3 = new EventData(Guid.NewGuid(), "Event3", JsonSerializer.Serialize(new { test = "data3" }));

        await _eventStore!.AppendToStreamAsync(streamName, new[] { event1, event2, event3 }, CancellationToken.None);

        // Act - subscribe with anchor set to event1's ID
        var receivedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var events = await _eventStore.ReadStreamAsync(streamName, CancellationToken.None);

        events.Should().HaveCount(3);
        events[0].EventId.Should().Be(event1.EventId);
        events[1].EventId.Should().Be(event2.EventId);
        events[2].EventId.Should().Be(event3.EventId);

        await foreach (var evt in _eventStore.SubscribeToStreamAsync(streamName, event1.EventId, cts.Token))
        {
            receivedEvents.Add(evt);
            if (receivedEvents.Count >= 2)
            {
                break;
            }
        }

        // Assert - should receive event2 and event3, but not event1
        receivedEvents.Should().HaveCount(2);
        receivedEvents[0].EventId.Should().Be(event2.EventId);
        receivedEvents[1].EventId.Should().Be(event3.EventId);
    }
}
