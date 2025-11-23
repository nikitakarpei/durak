namespace Rpc.Client;

/// <summary>
/// Wraps an IClient and manages the lifecycle of additional owned resources.
/// Disposes all resources in reverse order of registration.
/// </summary>
public sealed class CompositeDisposableClient : IClient
{
    private readonly IClient _innerClient;
    private readonly IDisposable[] _ownedResources;
    private bool _disposed;

    public CompositeDisposableClient(IClient innerClient, params IDisposable[] ownedResources)
    {
        _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        _ownedResources = ownedResources ?? throw new ArgumentNullException(nameof(ownedResources));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _innerClient.Dispose();

        for (int i = _ownedResources.Length - 1; i >= 0; i--)
        {
            _ownedResources[i]?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public Task<TResponse> UnaryCall<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
        where TRequest : Google.Protobuf.IMessage
        where TResponse : class, Google.Protobuf.IMessage, new()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CompositeDisposableClient));
        return _innerClient.UnaryCall<TRequest, TResponse>(method, request, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> StreamingCall<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
        where TRequest : Google.Protobuf.IMessage
        where TResponse : class, Google.Protobuf.IMessage, new()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(CompositeDisposableClient));
        return _innerClient.StreamingCall<TRequest, TResponse>(method, request, cancellationToken);
    }
}
