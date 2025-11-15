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
    /// If anchor is null, only yields new events as they are appended.
    /// If anchor is provided, yields all events after the specified event ID (both existing and new).
    public IAsyncEnumerable<RecordedEvent> SubscribeToStreamAsync(
        string streamId,
        Guid? anchor,
        CancellationToken cancellationToken
    );
}
