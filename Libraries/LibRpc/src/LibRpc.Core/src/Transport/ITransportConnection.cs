namespace Rpc.Transport;

public interface ITransportConnection : IDisposable
{
    public Guid Id { get; }
    public Task SendAsync(Protocol.RpcPacket packet, CancellationToken cancellationToken);
    public event Action<Protocol.RpcPacket>? PacketReceived;
    public event Action? Closed;
}
