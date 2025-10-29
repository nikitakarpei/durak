namespace Rpc.Server;

internal interface IStreamingCallHandler : ICallHandler
{
    public IAsyncEnumerable<Google.Protobuf.IMessage> HandleAsync(
        Google.Protobuf.WellKnownTypes.Any request,
        CancellationToken cancellationToken
    );
}

public interface IStreamingCallHandler<in TRequest, out TResponse>
    where TRequest : class, Google.Protobuf.IMessage, new()
    where TResponse : Google.Protobuf.IMessage
{
    public IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

internal class StreamingCallHandlerAdapter<TRequest, TResponse>(IStreamingCallHandler<TRequest, TResponse> handler)
    : IStreamingCallHandler
    where TRequest : class, Google.Protobuf.IMessage, new()
    where TResponse : Google.Protobuf.IMessage
{
    public IAsyncEnumerable<Google.Protobuf.IMessage> HandleAsync(
        Google.Protobuf.WellKnownTypes.Any request,
        CancellationToken cancellationToken
    )
    {
        var typedRequest = request.Unpack<TRequest>();
        return handler
            .HandleAsync(typedRequest, cancellationToken)
            .Select(response => (Google.Protobuf.IMessage)response);
    }
}
