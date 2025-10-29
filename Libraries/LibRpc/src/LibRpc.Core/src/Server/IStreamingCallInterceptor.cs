namespace Rpc.Server;

/// <summary>
/// Interceptor for streaming RPC calls. Allows intercepting and modifying requests and responses.
/// </summary>
public interface IStreamingCallInterceptor
{
    IAsyncEnumerable<Google.Protobuf.IMessage> InterceptStreamingAsync(
        IServiceProvider serviceProvider,
        Google.Protobuf.WellKnownTypes.Any request,
        StreamingCallInterceptorDelegate next,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// Delegate for the next interceptor or handler in the streaming call chain.
/// </summary>
public delegate IAsyncEnumerable<Google.Protobuf.IMessage> StreamingCallInterceptorDelegate(
    Google.Protobuf.WellKnownTypes.Any request,
    CancellationToken cancellationToken
);
