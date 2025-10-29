using System.Net.WebSockets;
using System.Text;
using Rpc.Transport;

namespace Rpc.Net.Transport;

public class ClientWebSocketConnection : ITransportConnection
{
    public Guid Id { get; init; }

    public event Action<Protocol.RpcPacket>? PacketReceived;
    public event Action? Closed;

    private readonly ClientWebSocket _webSocket;
    private readonly CancellationTokenSource _receiveCts;
    private bool _disposed;

    public ClientWebSocketConnection(Guid id, ClientWebSocket webSocket)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not open");
        }

        Id = id;

        _webSocket = webSocket;
        _receiveCts = new CancellationTokenSource();

        Task.Run(() => ReceiveLoop(_receiveCts.Token), _receiveCts.Token);
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by server",
                        CancellationToken.None
                    );
                    break;
                }

                Protocol.RpcPacket? packet = null;

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    packet = Protocol.RpcPacket.FromRawBytes(buffer.AsSpan(0, result.Count).ToArray());
                }

                if (packet is not null)
                {
                    PacketReceived?.Invoke(packet.Value);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (WebSocketException)
        {
            // Connection error
        }
    }

    public async Task SendAsync(Protocol.RpcPacket packet, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ClientWebSocketConnection));

        await _webSocket.SendAsync(
            new ArraySegment<byte>(packet.AsRawBytes()),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            cancellationToken
        );
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _receiveCts.Cancel();
        _receiveCts.Dispose();

        Closed?.Invoke();

        GC.SuppressFinalize(this);
    }
}
