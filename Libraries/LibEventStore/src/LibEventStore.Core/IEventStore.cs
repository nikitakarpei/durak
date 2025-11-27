namespace EventStore;

public interface IEventStore
{
    public Task AppendToStreamAsync(
        string streamId,
        IEnumerable<EventData> events,
        CancellationToken cancellationToken
    );

    /// Returns all existing events in the stream in the order they were appended.
    public Task<IList<RecordedEvent>> ReadStreamAsync(string streamId, CancellationToken cancellationToken);

    /// Subscribes to new events in the stream.
    /// If fromEventNumber is null, only yields new events as they are appended.
    /// If fromEventNumber is provided, yields all events starting from that event number (both existing and new).
    /// Pass 0 to get all events from the beginning of the stream.
    public IAsyncEnumerable<RecordedEvent> SubscribeToStreamAsync(
        string streamId,
        int? fromEventNumber,
        CancellationToken cancellationToken
    );
}
