using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EventStore.InMemory;

public sealed class InMemoryEventStore : IEventStore
{
    private class StreamState
    {
        public ImmutableList<EventData> Events { get; set; } = [];
        public List<Channel<EventData>> Subscribers { get; } = [];
    }

    private readonly ConcurrentDictionary<string, StreamState> _streams = new();

    public Task AppendToStreamAsync(string streamId, IEnumerable<EventData> events, CancellationToken cancellationToken)
    {
        var state = _streams.GetOrAdd(streamId, _ => new StreamState());

        lock (state)
        {
            state.Events = state.Events.AddRange(events);
            foreach (var subscriber in state.Subscribers.ToList())
            {
                foreach (var e in events)
                {
                    if (!subscriber.Writer.TryWrite(e))
                    {
                        // Clean up closed channels
                        state.Subscribers.Remove(subscriber);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<StreamSubscription> SubscribeToStreamAsync(string streamId, CancellationToken cancellationToken)
    {
        var state = _streams.GetOrAdd(streamId, _ => new StreamState());

        // Setup phase - create channel and register subscriber
        var channel = Channel.CreateUnbounded<EventData>();

        lock (state)
        {
            state.Subscribers.Add(channel);
        }

        // Take snapshot of existing events
        ImmutableList<EventData> snapshot;
        lock (state)
        {
            snapshot = state.Events;
        }

        // Factory for existing events
        async IAsyncEnumerable<RecordedEvent> ExistingEventsFactory([EnumeratorCancellation] CancellationToken ct)
        {
            // Yield existing events
            foreach (var eventData in snapshot)
            {
                yield return new RecordedEvent(eventData.EventId, eventData.Type, eventData.JsonData);
            }
            await Task.CompletedTask;
        }

        // Factory for new events
        async IAsyncEnumerable<RecordedEvent> NewEventsFactory([EnumeratorCancellation] CancellationToken ct)
        {
            // Yield live events from channel
            var reader = channel.Reader;
            while (await reader.WaitToReadAsync(ct))
            {
                while (reader.TryRead(out var eventData))
                {
                    yield return new RecordedEvent(eventData.EventId, eventData.Type, eventData.JsonData);
                }
            }
        }

        // Cleanup action
        ValueTask DisposeAction()
        {
            lock (state)
            {
                state.Subscribers.Remove(channel);
            }
            return ValueTask.CompletedTask;
        }

        return Task.FromResult(new StreamSubscription(ExistingEventsFactory, NewEventsFactory, DisposeAction));
    }
}
