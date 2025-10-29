using System.Net.WebSockets;
using System.Text;

namespace GameService.Web.Adapters;

internal class WebSocketAdapter(WebSocket webSocket) : IDisposable
{
    public event Action<byte[]>? MessageReceived;

    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public void Dispose()
    {
        _sendLock.Dispose();
        webSocket.Dispose();
    }

    public async Task SendAsync(byte[] message, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(message),
                WebSocketMessageType.Binary,
                true,
                cancellationToken
            );
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task PollAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBytes = new List<byte>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (receiveResult.CloseStatus.HasValue)
            {
                await webSocket.CloseAsync(
                    receiveResult.CloseStatus.Value,
                    receiveResult.CloseStatusDescription,
                    cancellationToken
                );
                return;
            }

            if (receiveResult.MessageType != WebSocketMessageType.Binary)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InvalidMessageType,
                    "Only binary messages are supported",
                    cancellationToken
                );
                return;
            }

            messageBytes.AddRange(buffer.Take(receiveResult.Count));

            if (receiveResult.EndOfMessage)
            {
                MessageReceived?.Invoke(messageBytes.ToArray());
                messageBytes.Clear();
            }
        }
    }
}
