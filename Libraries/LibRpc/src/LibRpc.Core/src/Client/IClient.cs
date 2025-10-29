namespace Rpc.Client;

public interface IClient : IDisposable
{
    public Task<TResponse> UnaryCall<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
        where TRequest : Google.Protobuf.IMessage
        where TResponse : class, Google.Protobuf.IMessage, new();

    public IAsyncEnumerable<TResponse> StreamingCall<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken = default
    )
        where TRequest : Google.Protobuf.IMessage
        where TResponse : class, Google.Protobuf.IMessage, new();
}
