namespace EventStore;

public interface IEventStore
{
    public Task AppendToStreamAsync(
        string streamName,
        IEnumerable<EventData> events,
        CancellationToken cancellationToken
    );

    /// Returns a subscription to events in the order they were appended to the stream and new events as they are appended.
    /// The subscription is ready to use when returned.
    public Task<StreamSubscription> SubscribeToStreamAsync(string streamName, CancellationToken cancellationToken);
}
