using System.Net.Http.Headers;
using Rpc.Transport;

namespace Rpc.Net.Transport;

public class ClientHttpConnection(Guid id, HttpClient httpClient, Uri uri) : ITransportConnection
{
    public Guid Id { get; init; } = id;

    public event Action<Protocol.RpcPacket>? PacketReceived;
    public event Action? Closed;

    private bool _disposed;

    public async Task SendAsync(Protocol.RpcPacket packet, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ClientHttpConnection));

        var content = new ByteArrayContent(packet.AsRawBytes());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await httpClient.PostAsync(uri, content, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var responsePacket = Protocol.RpcPacket.FromRawBytes(responseContent);
            PacketReceived?.Invoke(responsePacket);
        }
        else
        {
            throw new HttpRequestException($"HTTP request failed with status {response.StatusCode}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Closed?.Invoke();

        GC.SuppressFinalize(this);
    }
}
