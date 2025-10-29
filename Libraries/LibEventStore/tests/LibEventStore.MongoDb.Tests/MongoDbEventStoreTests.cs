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

        // Assert - verify by subscribing
        var receivedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await using var subscription = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);
        await foreach (var evt in subscription.GetEvents(cts.Token))
        {
            receivedEvents.Add(evt);
            if (evt is ExistingRecordedEvent { IsLast: true })
            {
                break;
            }
        }

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
        var receivedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await using var subscription = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);
        await foreach (var evt in subscription.GetEvents(cts.Token))
        {
            receivedEvents.Add(evt);
            if (evt is ExistingRecordedEvent { IsLast: true })
            {
                break;
            }
        }

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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var subscription = await _eventStore!.SubscribeToStreamAsync(streamName, cts.Token);

        var subscribeTask = Task.Run(async () =>
        {
            await foreach (var evt in subscription.GetEvents(cts.Token))
            {
                subscribedEvents.Add(evt);
                if (subscribedEvents.Count >= 1)
                {
                    break;
                }
            }
        });

        // Append event - no delay needed, subscription is already ready!
        await _eventStore!.AppendToStreamAsync(
            streamName,
            new[] { new EventData(eventId, "NewEvent", JsonSerializer.Serialize(new { test = "data" })) },
            CancellationToken.None
        );

        await subscribeTask;

        // Assert
        subscribedEvents.Should().HaveCount(1);
        subscribedEvents[0].Should().BeOfType<NewRecordedEvent>();
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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var subscription = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);

        var subscribeTask = Task.Run(async () =>
        {
            await foreach (var evt in subscription.GetEvents(cts.Token))
            {
                receivedEvents.Add(evt);
                if (receivedEvents.Count >= 2)
                {
                    break;
                }
            }
        });

        // Append new event - no delay needed, subscription is already ready!
        await _eventStore!.AppendToStreamAsync(streamName, new[] { newEvent }, CancellationToken.None);

        await subscribeTask;

        // Assert
        receivedEvents.Should().HaveCount(2);
        receivedEvents[0].Should().BeOfType<ExistingRecordedEvent>();
        receivedEvents[0].EventId.Should().Be(existingEvent.EventId);
        receivedEvents[1].Should().BeOfType<NewRecordedEvent>();
        receivedEvents[1].EventId.Should().Be(newEvent.EventId);
    }

    [Fact]
    public async Task SubscribeToStreamAsync_MultipleSubscribers_ShouldAllReceiveEvents()
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

        // Act - multiple subscribers
        var subscriber1Events = new List<RecordedEvent>();
        var subscriber2Events = new List<RecordedEvent>();
        var subscriber3Events = new List<RecordedEvent>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var subscription1 = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);
        await using var subscription2 = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);
        await using var subscription3 = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);

        var sub1Task = Task.Run(async () =>
        {
            await foreach (var evt in subscription1.GetEvents(cts.Token))
            {
                subscriber1Events.Add(evt);
                if (evt is ExistingRecordedEvent { IsLast: true })
                {
                    break;
                }
            }
        });

        var sub2Task = Task.Run(async () =>
        {
            await foreach (var evt in subscription2.GetEvents(cts.Token))
            {
                subscriber2Events.Add(evt);
                if (evt is ExistingRecordedEvent { IsLast: true })
                {
                    break;
                }
            }
        });

        var sub3Task = Task.Run(async () =>
        {
            await foreach (var evt in subscription3.GetEvents(cts.Token))
            {
                subscriber3Events.Add(evt);
                if (evt is ExistingRecordedEvent { IsLast: true })
                {
                    break;
                }
            }
        });

        await Task.WhenAll(sub1Task, sub2Task, sub3Task);

        // Assert
        subscriber1Events.Should().HaveCount(5);
        subscriber2Events.Should().HaveCount(5);
        subscriber3Events.Should().HaveCount(5);

        var expectedIds = events.Select(e => e.EventId).ToList();
        subscriber1Events.Select(e => e.EventId).Should().BeEquivalentTo(expectedIds);
        subscriber2Events.Select(e => e.EventId).Should().BeEquivalentTo(expectedIds);
        subscriber3Events.Select(e => e.EventId).Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task SubscribeToStreamAsync_IsLastFlag_ShouldBeCorrect()
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

        // Act
        var receivedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await using var subscription = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);
        await foreach (var evt in subscription.GetEvents(cts.Token))
        {
            receivedEvents.Add(evt);
            if (evt is ExistingRecordedEvent { IsLast: true })
            {
                break;
            }
        }

        // Assert
        receivedEvents.Should().HaveCount(5);
        for (int i = 0; i < 4; i++)
        {
            receivedEvents[i].Should().BeOfType<ExistingRecordedEvent>().Which.IsLast.Should().BeFalse();
        }
        receivedEvents[4].Should().BeOfType<ExistingRecordedEvent>().Which.IsLast.Should().BeTrue();
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
        var stream1Events = new List<RecordedEvent>();
        var stream2Events = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await using var subscription1 = await _eventStore.SubscribeToStreamAsync(stream1, cts.Token);
        await foreach (var evt in subscription1.GetEvents(cts.Token))
        {
            stream1Events.Add(evt);
            if (evt is ExistingRecordedEvent { IsLast: true })
            {
                break;
            }
        }

        await using var subscription2 = await _eventStore.SubscribeToStreamAsync(stream2, cts.Token);
        await foreach (var evt in subscription2.GetEvents(cts.Token))
        {
            stream2Events.Add(evt);
            if (evt is ExistingRecordedEvent { IsLast: true })
            {
                break;
            }
        }

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
        var events = Enumerable
            .Range(1, 3)
            .Select(i => new EventData(
                Guid.NewGuid(),
                $"Event{i}",
                JsonSerializer.Serialize(new { test = $"data{i}" })
            ))
            .ToList();

        await _eventStore!.AppendToStreamAsync(streamName, events, CancellationToken.None);

        // Act
        var receivedEvents = new List<RecordedEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        await using var subscription = await _eventStore.SubscribeToStreamAsync(streamName, cts.Token);

        try
        {
            await foreach (var evt in subscription.GetEvents(cts.Token))
            {
                receivedEvents.Add(evt);
                // Don't break, let cancellation happen
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - should have received existing events before cancellation
        receivedEvents.Should().HaveCount(3);
    }
}
