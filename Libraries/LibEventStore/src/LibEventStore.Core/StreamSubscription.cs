namespace EventStore;

public class StreamSubscription : IAsyncDisposable
{
    private readonly Func<CancellationToken, IAsyncEnumerable<RecordedEvent>> _existingEventsFactory;
    private readonly Func<CancellationToken, IAsyncEnumerable<RecordedEvent>> _newEventsFactory;
    private readonly Func<ValueTask>? _disposeAction;

    public StreamSubscription(
        Func<CancellationToken, IAsyncEnumerable<RecordedEvent>> existingEventsFactory,
        Func<CancellationToken, IAsyncEnumerable<RecordedEvent>> newEventsFactory,
        Func<ValueTask>? disposeAction = null
    )
    {
        _existingEventsFactory = existingEventsFactory;
        _newEventsFactory = newEventsFactory;
        _disposeAction = disposeAction;
    }

    public IAsyncEnumerable<RecordedEvent> GetExistingEvents(CancellationToken cancellationToken) =>
        _existingEventsFactory(cancellationToken);

    public IAsyncEnumerable<RecordedEvent> GetNewEvents(CancellationToken cancellationToken) =>
        _newEventsFactory(cancellationToken);

    public ValueTask DisposeAsync() => _disposeAction?.Invoke() ?? ValueTask.CompletedTask;
}
