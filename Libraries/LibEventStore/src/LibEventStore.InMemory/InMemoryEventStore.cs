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

    public Task<IList<RecordedEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken)
    {
        var state = _streams.GetOrAdd(streamId, _ => new StreamState());

        ImmutableList<EventData> snapshot;
        lock (state)
        {
            snapshot = state.Events;
        }

        var events = snapshot.Select((e, index) => new RecordedEvent(e.EventId, index, e.Type, e.JsonData)).ToList();

        return Task.FromResult<IList<RecordedEvent>>(events);
    }

    public async IAsyncEnumerable<RecordedEvent> SubscribeToStreamAsync(
        string streamId,
        int? fromEventNumber,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var state = _streams.GetOrAdd(streamId, _ => new StreamState());

        // Setup phase - create channel and register subscriber
        var channel = Channel.CreateUnbounded<EventData>();

        lock (state)
        {
            state.Subscribers.Add(channel);
        }

        try
        {
            // If fromEventNumber is provided, yield existing events starting from that event number
            if (fromEventNumber.HasValue)
            {
                ImmutableList<EventData> snapshot;
                lock (state)
                {
                    snapshot = state.Events;
                }

                // Skip events before fromEventNumber
                var eventsToYield = snapshot.Skip(fromEventNumber.Value);
                int currentEventNumber = fromEventNumber.Value;

                foreach (var eventData in eventsToYield)
                {
                    yield return new RecordedEvent(
                        eventData.EventId,
                        currentEventNumber,
                        eventData.Type,
                        eventData.JsonData
                    );
                    currentEventNumber++;
                }
            }

            // Yield new events from channel
            var reader = channel.Reader;
            int nextEventNumber;
            lock (state)
            {
                nextEventNumber = state.Events.Count;
            }

            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var eventData))
                {
                    yield return new RecordedEvent(
                        eventData.EventId,
                        nextEventNumber,
                        eventData.Type,
                        eventData.JsonData
                    );
                    nextEventNumber++;
                }
            }
        }
        finally
        {
            // Cleanup
            lock (state)
            {
                state.Subscribers.Remove(channel);
            }
        }
    }
}
