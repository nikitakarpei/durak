namespace Rpc.Server;

/// <summary>
/// Interceptor for unary RPC calls. Allows intercepting and modifying requests and responses.
/// </summary>
public interface IUnaryCallInterceptor
{
    Task<Google.Protobuf.IMessage> InterceptUnaryAsync(
        IServiceProvider serviceProvider,
        Google.Protobuf.WellKnownTypes.Any request,
        UnaryCallInterceptorDelegate next,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// Delegate for the next interceptor or handler in the unary call chain.
/// </summary>
public delegate Task<Google.Protobuf.IMessage> UnaryCallInterceptorDelegate(
    Google.Protobuf.WellKnownTypes.Any request,
    CancellationToken cancellationToken
);
