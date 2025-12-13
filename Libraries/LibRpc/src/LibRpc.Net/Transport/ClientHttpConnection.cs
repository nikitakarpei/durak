using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using Microsoft.Extensions.Logging;
using Rpc.Transport;

namespace Rpc.Net.Transport;

public class ClientHttpConnection(
    ILogger<ClientHttpConnection> logger,
    Guid id,
    HttpClient httpClient,
    Uri uri,
    Version? minimumHttpVersion = null
) : ITransportConnection
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

        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = content,
            Version = minimumHttpVersion ?? new Version(1, 1),
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var parser = SseParser.Create(stream);

                    await foreach (var item in parser.EnumerateAsync(cancellationToken))
                    {
                        if (string.IsNullOrEmpty(item.Data))
                        {
                            continue;
                        }

                        var packetBytes = Convert.FromBase64String(item.Data);
                        var responsePacket = Protocol.RpcPacket.FromRawBytes(packetBytes);
                        logger.LogDebug("[{Id}] Received packet from SSE stream", Id);
                        PacketReceived?.Invoke(responsePacket);
                    }

                    logger.LogDebug("[{Id}] End of SSE stream", Id);
                }
                catch (OperationCanceledException e)
                {
                    logger.LogDebug(e, "[{Id}] Operation canceled while reading SSE stream", Id);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "[{Id}] Error reading SSE stream", Id);
                    Closed?.Invoke();
                }
            },
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

        Closed?.Invoke();

        GC.SuppressFinalize(this);
    }
}
