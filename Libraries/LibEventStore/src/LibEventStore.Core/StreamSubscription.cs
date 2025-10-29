using System.Runtime.CompilerServices;

namespace EventStore;

public class StreamSubscription : IAsyncDisposable
{
    private readonly Func<CancellationToken, IAsyncEnumerable<RecordedEvent>> _eventsFactory;
    private readonly Func<ValueTask>? _disposeAction;

    public StreamSubscription(
        Func<CancellationToken, IAsyncEnumerable<RecordedEvent>> eventsFactory,
        Func<ValueTask>? disposeAction = null
    )
    {
        _eventsFactory = eventsFactory;
        _disposeAction = disposeAction;
    }

    public IAsyncEnumerable<RecordedEvent> GetEvents(CancellationToken cancellationToken = default) =>
        _eventsFactory(cancellationToken);

    public ValueTask DisposeAsync() => _disposeAction?.Invoke() ?? ValueTask.CompletedTask;
}
