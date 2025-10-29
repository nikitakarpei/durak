using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Rpc.Server;

public class RpcRequestHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RpcRequestHandler> _logger;
    private readonly CallHandlerRegistry _registry;
    private readonly IEnumerable<IUnaryCallInterceptor> _unaryInterceptors;
    private readonly IEnumerable<IStreamingCallInterceptor> _streamingInterceptors;

    public RpcRequestHandler(
        IServiceProvider serviceProvider,
        ILogger<RpcRequestHandler> logger,
        CallHandlerRegistry registry,
        IEnumerable<IUnaryCallInterceptor> unaryInterceptors,
        IEnumerable<IStreamingCallInterceptor> streamingInterceptors
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _registry = registry;
        _unaryInterceptors = unaryInterceptors;
        _streamingInterceptors = streamingInterceptors;
    }

    public IAsyncEnumerable<Protocol.RpcPacket> HandleAsync(
        Protocol.RpcPacket packet,
        CancellationToken cancellationToken
    )
    {
        var envelope = ParseClientEnvelope(packet);

        if (envelope.Payload.Is(Protocol.RpcInvoke.Descriptor))
        {
            var invoke = envelope.Payload.Unpack<Protocol.RpcInvoke>();

            return HandleInvoke(invoke, cancellationToken);
        }

        throw new InvalidOperationException($"Unknown message type {envelope.Payload.TypeUrl}");
    }

    private async IAsyncEnumerable<Protocol.RpcPacket> HandleInvoke(
        Protocol.RpcInvoke invoke,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (!_registry.TryGetHandler(invoke.Method, _serviceProvider, out var handler))
        {
            _logger.LogWarning(
                "Received invoke for unimplemented method {Method} with callId {CallId}",
                invoke.Method,
                invoke.CallId
            );

            yield return MakeErrorResponse(invoke.CallId, Protocol.RpcServerErrorCode.NotImplemented);
            yield break;
        }

        var responses = HandleCall(
            _serviceProvider,
            invoke.Method,
            invoke.CallId,
            invoke.Data,
            handler,
            cancellationToken
        );

        await using var responsesEnumerator = responses.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            Protocol.RpcPacket response;

            try
            {
                if (!await responsesEnumerator.MoveNextAsync(cancellationToken))
                {
                    break;
                }
                response = responsesEnumerator.Current;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception occurred while handling call with method {Method} and callId {CallId}",
                    invoke.Method,
                    invoke.CallId
                );

                response = MakeErrorResponse(invoke.CallId, Protocol.RpcServerErrorCode.Internal);
            }

            yield return response;
        }
    }

    private async IAsyncEnumerable<Protocol.RpcPacket> HandleCall(
        IServiceProvider serviceProvider,
        string method,
        string callId,
        Google.Protobuf.WellKnownTypes.Any data,
        ICallHandler handler,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        if (handler is IUnaryCallHandler unaryCallHandler)
        {
            yield return await HandleUnaryCall(serviceProvider, callId, data, unaryCallHandler, cancellationToken);
            yield break;
        }

        if (handler is IStreamingCallHandler streamingCallHandler)
        {
            var responses = HandleStreamingCall(serviceProvider, callId, data, streamingCallHandler, cancellationToken);

            await foreach (var response in responses)
            {
                yield return response;
            }
            yield break;
        }

        throw new NotImplementedException($"Unknown registration type for method {method} with callId {callId}");
    }

    private async Task<Protocol.RpcPacket> HandleUnaryCall(
        IServiceProvider serviceProvider,
        string callId,
        Google.Protobuf.WellKnownTypes.Any data,
        IUnaryCallHandler handler,
        CancellationToken cancellationToken
    )
    {
        var response = await ExecuteUnaryCallWithInterceptors(serviceProvider, data, handler, cancellationToken);

        return MakeUnaryCallResponse(callId, response);
    }

    private async IAsyncEnumerable<Protocol.RpcPacket> HandleStreamingCall(
        IServiceProvider serviceProvider,
        string callId,
        Google.Protobuf.WellKnownTypes.Any data,
        IStreamingCallHandler handler,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (
            var response in ExecuteStreamingCallWithInterceptors(serviceProvider, data, handler, cancellationToken)
        )
        {
            yield return MakeStreamingCallResponse(callId, response);
        }

        yield return MakeStreamingFinishedResponse(callId);
    }

    private static Protocol.RpcClientEnvelope ParseClientEnvelope(Protocol.RpcPacket packet)
    {
        try
        {
            return Protocol.RpcClientEnvelope.Parser.ParseFrom(packet.AsRawBytes());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse client envelope", ex);
        }
    }

    private static Protocol.RpcPacket MakeErrorResponse(string callId, Protocol.RpcServerErrorCode code)
    {
        var rpcError = new Protocol.RpcServerError { CallId = callId, Code = code };

        var envelope = new Protocol.RpcServerEnvelope { Payload = Google.Protobuf.WellKnownTypes.Any.Pack(rpcError) };

        return Protocol.RpcPacket.FromProtoMessage(envelope);
    }

    private static Protocol.RpcPacket MakeUnaryCallResponse(string callId, Google.Protobuf.IMessage response)
    {
        var rpcResponse = new Protocol.RpcServerUnaryCallResponse
        {
            CallId = callId,
            Data = Google.Protobuf.WellKnownTypes.Any.Pack(response),
        };

        var envelope = new Protocol.RpcServerEnvelope
        {
            Payload = Google.Protobuf.WellKnownTypes.Any.Pack(rpcResponse),
        };

        return Protocol.RpcPacket.FromProtoMessage(envelope);
    }

    private static Protocol.RpcPacket MakeStreamingCallResponse(string callId, Google.Protobuf.IMessage response)
    {
        var rpcResponse = new Protocol.RpcServerStreamingCallResponse
        {
            CallId = callId,
            Data = Google.Protobuf.WellKnownTypes.Any.Pack(response),
        };

        var envelope = new Protocol.RpcServerEnvelope
        {
            Payload = Google.Protobuf.WellKnownTypes.Any.Pack(rpcResponse),
        };

        return Protocol.RpcPacket.FromProtoMessage(envelope);
    }

    private static Protocol.RpcPacket MakeStreamingFinishedResponse(string callId)
    {
        var rpcFinished = new Protocol.RpcServerStreamingCallFinished { CallId = callId };

        var envelope = new Protocol.RpcServerEnvelope
        {
            Payload = Google.Protobuf.WellKnownTypes.Any.Pack(rpcFinished),
        };

        return Protocol.RpcPacket.FromProtoMessage(envelope);
    }

    private async Task<Google.Protobuf.IMessage> ExecuteUnaryCallWithInterceptors(
        IServiceProvider serviceProvider,
        Google.Protobuf.WellKnownTypes.Any request,
        IUnaryCallHandler handler,
        CancellationToken cancellationToken
    )
    {
        if (!_unaryInterceptors.Any())
        {
            return await handler.HandleAsync(request, cancellationToken);
        }

        var interceptorIndex = 0;
        return await ExecuteUnaryInterceptor(serviceProvider, request, handler, cancellationToken, interceptorIndex);
    }

    private async Task<Google.Protobuf.IMessage> ExecuteUnaryInterceptor(
        IServiceProvider serviceProvider,
        Google.Protobuf.WellKnownTypes.Any request,
        IUnaryCallHandler handler,
        CancellationToken cancellationToken,
        int interceptorIndex
    )
    {
        if (interceptorIndex >= _unaryInterceptors.Count())
        {
            return await handler.HandleAsync(request, cancellationToken);
        }

        var interceptor = _unaryInterceptors.ElementAt(interceptorIndex);
        return await interceptor.InterceptUnaryAsync(
            serviceProvider,
            request,
            (req, ct) => ExecuteUnaryInterceptor(serviceProvider, req, handler, ct, interceptorIndex + 1),
            cancellationToken
        );
    }

    private IAsyncEnumerable<Google.Protobuf.IMessage> ExecuteStreamingCallWithInterceptors(
        IServiceProvider serviceProvider,
        Google.Protobuf.WellKnownTypes.Any request,
        IStreamingCallHandler handler,
        CancellationToken cancellationToken
    )
    {
        if (!_streamingInterceptors.Any())
        {
            return handler.HandleAsync(request, cancellationToken);
        }

        var interceptorIndex = 0;
        return ExecuteStreamingInterceptor(serviceProvider, request, handler, cancellationToken, interceptorIndex);
    }

    private IAsyncEnumerable<Google.Protobuf.IMessage> ExecuteStreamingInterceptor(
        IServiceProvider serviceProvider,
        Google.Protobuf.WellKnownTypes.Any request,
        IStreamingCallHandler handler,
        CancellationToken cancellationToken,
        int interceptorIndex
    )
    {
        if (interceptorIndex >= _streamingInterceptors.Count())
        {
            return handler.HandleAsync(request, cancellationToken);
        }

        var interceptor = _streamingInterceptors.ElementAt(interceptorIndex);
        return interceptor.InterceptStreamingAsync(
            serviceProvider,
            request,
            (req, ct) => ExecuteStreamingInterceptor(serviceProvider, req, handler, ct, interceptorIndex + 1),
            cancellationToken
        );
    }
}
