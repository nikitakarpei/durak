using System.Threading.Channels;

namespace Rpc.Client;

public interface IStreamingPendingCall : IPendingCall
{
    void WriteResponse(Google.Protobuf.WellKnownTypes.Any packedResponse);
    void Complete();
}

internal class PendingStreamingCall<TResponse> : IStreamingPendingCall
    where TResponse : class, Google.Protobuf.IMessage, new()
{
    private readonly ChannelWriter<TResponse> _writer;
    private readonly CancellationTokenRegistration _cancellationRegistration;
    private bool _disposed;

    public PendingStreamingCall(ChannelWriter<TResponse> writer, CancellationTokenRegistration cancellationRegistration)
    {
        _writer = writer;
        _cancellationRegistration = cancellationRegistration;
    }

    public void WriteResponse(Google.Protobuf.WellKnownTypes.Any packedResponse)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PendingStreamingCall<TResponse>));

        try
        {
            var response = packedResponse.Unpack<TResponse>();
            _writer.TryWrite(response);
        }
        catch (Exception ex)
        {
            _writer.Complete(new RpcException("Failed to unpack response", ex));
            _cancellationRegistration.Unregister();
        }
    }

    public void Complete()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PendingStreamingCall<TResponse>));

        _writer.Complete();
        _cancellationRegistration.Unregister();
    }

    public void SetException(Exception exception)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PendingStreamingCall<TResponse>));

        _writer.Complete(exception);
        _cancellationRegistration.Unregister();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellationRegistration.Dispose();
    }
}
