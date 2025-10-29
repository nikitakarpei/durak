namespace Rpc.Server;

internal interface IUnaryCallHandler : ICallHandler
{
    public Task<Google.Protobuf.IMessage> HandleAsync(
        Google.Protobuf.WellKnownTypes.Any request,
        CancellationToken cancellationToken
    );
}

public interface IUnaryCallHandler<in TRequest, TResponse>
    where TRequest : class, Google.Protobuf.IMessage, new()
    where TResponse : Google.Protobuf.IMessage
{
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

internal class UnaryCallHandlerAdapter<TRequest, TResponse>(IUnaryCallHandler<TRequest, TResponse> handler)
    : IUnaryCallHandler
    where TRequest : class, Google.Protobuf.IMessage, new()
    where TResponse : Google.Protobuf.IMessage
{
    public async Task<Google.Protobuf.IMessage> HandleAsync(
        Google.Protobuf.WellKnownTypes.Any request,
        CancellationToken cancellationToken
    )
    {
        var typedRequest = request.Unpack<TRequest>();
        return await handler.HandleAsync(typedRequest, cancellationToken);
    }
}
