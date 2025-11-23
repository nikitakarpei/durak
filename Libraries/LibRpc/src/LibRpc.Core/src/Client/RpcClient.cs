using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Rpc.Client;

public class RpcClient : IClient
{
    private readonly Transport.ITransportConnection _connection;
    private readonly ILogger<RpcClient> _logger;

    private readonly Dictionary<string, IPendingCall> _pendingCalls = [];
    private bool _disposed;

    public RpcClient(Transport.ITransportConnection connection, ILogger<RpcClient> logger)
    {
        _connection = connection;
        _logger = logger;

        _connection.PacketReceived += OnPacketReceived;
        _connection.Closed += OnConnectionClosed;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        CancelAllPendingCalls(new ClientDisposedException());

        GC.SuppressFinalize(this);
    }

    async Task<TResponse> IClient.UnaryCall<TRequest, TResponse>(
        string method,
        TRequest request,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RpcClient));

        var callId = CreateNewCallId();
        var promise = new TaskCompletionSource<TResponse>();

        var cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            if (_pendingCalls.Remove(callId, out var unaryCall))
            {
                unaryCall.SetException(new OperationCanceledException());
            }
        });

        using var pendingCall = new PendingUnaryCall<TResponse>(promise, cancellationTokenRegistration);
        _pendingCalls[callId] = pendingCall;

        try
        {
            await SendInvoke(callId, method, request, cancellationToken);

            _logger.LogDebug("Sent unary call {Method} with callId {CallId}", method, callId);

            return await promise.Task;
        }
        finally
        {
            _pendingCalls.Remove(callId);
        }
    }

    async IAsyncEnumerable<TResponse> IClient.StreamingCall<TRequest, TResponse>(
        string method,
        TRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RpcClient));

        var callId = CreateNewCallId();
        var channel = Channel.CreateUnbounded<TResponse>();

        var cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            if (_pendingCalls.Remove(callId, out var streamingCall))
            {
                streamingCall.SetException(new OperationCanceledException());
            }
        });

        using var pendingCall = new PendingStreamingCall<TResponse>(channel.Writer, cancellationTokenRegistration);
        _pendingCalls[callId] = pendingCall;

        try
        {
            await SendInvoke(callId, method, request, cancellationToken);

            _logger.LogDebug("Sent streaming call {Method} with callId {CallId}", method, callId);

            await foreach (var response in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return response;
            }
        }
        finally
        {
            _pendingCalls.Remove(callId);
        }
    }

    private void OnPacketReceived(Protocol.RpcPacket packet)
    {
        var envelope = ParseServerEnvelope(packet);

        if (envelope.Payload.Is(Protocol.RpcServerError.Descriptor))
        {
            HandleServerError(envelope.Payload.Unpack<Protocol.RpcServerError>());
            return;
        }
        if (envelope.Payload.Is(Protocol.RpcServerUnaryCallResponse.Descriptor))
        {
            HandleServerUnaryCallResponse(envelope.Payload.Unpack<Protocol.RpcServerUnaryCallResponse>());
            return;
        }
        if (envelope.Payload.Is(Protocol.RpcServerStreamingCallResponse.Descriptor))
        {
            HandleServerStreamingCallResponse(envelope.Payload.Unpack<Protocol.RpcServerStreamingCallResponse>());
            return;
        }
        if (envelope.Payload.Is(Protocol.RpcServerStreamingCallFinished.Descriptor))
        {
            HandleServerStreamingCallFinished(envelope.Payload.Unpack<Protocol.RpcServerStreamingCallFinished>());
            return;
        }

        _logger.LogWarning("Received unknown server message type {Message}", envelope.Payload.TypeUrl);
    }

    private void OnConnectionClosed()
    {
        CancelAllPendingCalls(new ConnectionClosedException());
    }

    private void CancelAllPendingCalls(Exception exception)
    {
        var calls = _pendingCalls.Values.ToList();
        _pendingCalls.Clear();

        foreach (var call in calls)
        {
            call.SetException(exception);
        }
    }

    private static string CreateNewCallId()
    {
        return Guid.NewGuid().ToString();
    }

    private async Task SendInvoke(
        string requestId,
        string method,
        Google.Protobuf.IMessage request,
        CancellationToken cancellationToken
    )
    {
        var rpcInvoke = new Protocol.RpcInvoke
        {
            CallId = requestId,
            Method = method,
            Data = Google.Protobuf.WellKnownTypes.Any.Pack(request),
        };

        var envelope = new Protocol.RpcClientEnvelope { Payload = Google.Protobuf.WellKnownTypes.Any.Pack(rpcInvoke) };

        await _connection.SendAsync(Protocol.RpcPacket.FromProtoMessage(envelope), cancellationToken);
    }

    private static Protocol.RpcServerEnvelope ParseServerEnvelope(Protocol.RpcPacket packet)
    {
        try
        {
            return Protocol.RpcServerEnvelope.Parser.ParseFrom(packet.AsRawBytes());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse server envelope", ex);
        }
    }

    private void HandleServerError(Protocol.RpcServerError error)
    {
        if (_pendingCalls.Remove(error.CallId, out var unaryCall))
        {
            unaryCall.SetException(ToRpcException(error.Code));
        }
    }

    private void HandleServerUnaryCallResponse(Protocol.RpcServerUnaryCallResponse response)
    {
        if (!_pendingCalls.Remove(response.CallId, out var pendingCall))
        {
            _logger.LogWarning("Received unary call response for unknown call {CallId}", response.CallId);
            return;
        }

        if (pendingCall is not IUnaryPendingCall unaryCall)
        {
            _logger.LogWarning("Received unary call response for non-unary pending call {CallId}", response.CallId);

            pendingCall.SetException(new ProtocolViolationException());
            return;
        }

        _logger.LogDebug("Received unary call response for call {CallId}", response.CallId);

        unaryCall.SetResponse(response.Data);
    }

    private void HandleServerStreamingCallResponse(Protocol.RpcServerStreamingCallResponse response)
    {
        if (!_pendingCalls.TryGetValue(response.CallId, out var pendingCall))
        {
            _logger.LogWarning("Received streaming call response for unknown call {CallId}", response.CallId);
            return;
        }

        if (pendingCall is not IStreamingPendingCall streamingCall)
        {
            _logger.LogWarning(
                "Received streaming call response for non-streaming pending call {CallId}",
                response.CallId
            );

            pendingCall.SetException(new ProtocolViolationException());
            return;
        }

        _logger.LogDebug("Received streaming call response for call {CallId}", response.CallId);

        streamingCall.WriteResponse(response.Data);
    }

    private void HandleServerStreamingCallFinished(Protocol.RpcServerStreamingCallFinished finished)
    {
        if (!_pendingCalls.Remove(finished.CallId, out var pendingCall))
        {
            _logger.LogWarning("Received streaming call finished for unknown call {CallId}", finished.CallId);
            return;
        }

        if (pendingCall is not IStreamingPendingCall streamingCall)
        {
            _logger.LogWarning(
                "Received streaming call finished for non-streaming pending call {CallId}",
                finished.CallId
            );

            pendingCall.SetException(new ProtocolViolationException());
            return;
        }

        _logger.LogDebug("Received streaming call finished for call {CallId}", finished.CallId);

        streamingCall.Complete();
    }

    private static RpcException ToRpcException(Protocol.RpcServerErrorCode code)
    {
        return code switch
        {
            Protocol.RpcServerErrorCode.NotImplemented => new MethodNotImplementedException(),
            Protocol.RpcServerErrorCode.MalformedRequest => new MalformedRequestException(),
            Protocol.RpcServerErrorCode.Internal => new ServerInternalException(),
            _ => new RpcException("Unknown error"),
        };
    }
}
