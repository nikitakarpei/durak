using System.Buffers.Binary;
using System.Net.Http.Headers;
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

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var packetBytes = await ReadPacketAsync(stream, cancellationToken);
                        if (packetBytes == null)
                        {
                            logger.LogDebug("[{Id}] End of stream", Id);
                            break;
                        }

                        var responsePacket = Protocol.RpcPacket.FromRawBytes(packetBytes);
                        logger.LogDebug("[{Id}] Received packet from stream", Id);
                        PacketReceived?.Invoke(responsePacket);
                    }
                }
                catch (OperationCanceledException e)
                {
                    logger.LogDebug(e, "[{Id}] Operation canceled while reading packet from stream", Id);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "[{Id}] Error reading packet from stream", Id);
                    Closed?.Invoke();
                }
            },
            cancellationToken
        );
    }

    private static async Task<byte[]?> ReadPacketAsync(Stream stream, CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        var bytesRead = await stream.ReadAsync(lengthBuffer.AsMemory(0, 4), cancellationToken);

        if (bytesRead == 0)
        {
            return null; // End of stream
        }

        if (bytesRead < 4)
        {
            throw new InvalidOperationException("Incomplete length prefix in stream");
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);

        if (length <= 0 || length > 100 * 1024 * 1024) // 100 MB sanity check
        {
            throw new InvalidOperationException($"Invalid packet length: {length}");
        }

        // Read packet data
        var packetBuffer = new byte[length];
        var totalBytesRead = 0;

        while (totalBytesRead < length)
        {
            bytesRead = await stream.ReadAsync(
                packetBuffer.AsMemory(totalBytesRead, length - totalBytesRead),
                cancellationToken
            );

            if (bytesRead == 0)
            {
                throw new InvalidOperationException("Unexpected end of stream while reading packet data");
            }

            totalBytesRead += bytesRead;
        }

        return packetBuffer;
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
