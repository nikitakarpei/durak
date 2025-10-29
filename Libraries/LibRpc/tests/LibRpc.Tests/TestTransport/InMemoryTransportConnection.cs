using System.Text;
using Rpc.Transport;

namespace Rpc.Tests.TestTransport;

// In-memory transport connection for testing
internal class InMemoryTransportConnection : ITransportConnection
{
    private readonly InMemoryTransportPipe _pipe;
    private bool _isClosed;

    public Guid Id { get; }
    public event Action<Protocol.RpcPacket>? PacketReceived;
    public event Action? Closed;

    public InMemoryTransportConnection(Guid id, InMemoryTransportPipe pipe)
    {
        Id = id;
        _pipe = pipe;
        _pipe.DataSent += OnPipeDataReceived;
    }

    public void Dispose()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        _pipe.DataSent -= OnPipeDataReceived;
    }

    private void OnPipeDataReceived(byte[] data)
    {
        if (_isClosed)
        {
            return;
        }
        PacketReceived?.Invoke(Protocol.RpcPacket.FromRawBytes(data));
    }

    public Task SendAsync(Protocol.RpcPacket packet, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isClosed, nameof(InMemoryTransportConnection));

        if (_isClosed)
        {
            throw new InvalidOperationException("Connection is closed");
        }

        var bytes = packet.AsRawBytes();
        _pipe.Send(bytes);
        return Task.CompletedTask;
    }

    public void Close()
    {
        ObjectDisposedException.ThrowIf(_isClosed, nameof(InMemoryTransportConnection));

        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        Closed?.Invoke();
        _pipe.DataSent -= OnPipeDataReceived;
    }
}
