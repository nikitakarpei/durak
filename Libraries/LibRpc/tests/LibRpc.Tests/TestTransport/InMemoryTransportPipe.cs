using System.Threading.Channels;

namespace Rpc.Tests.TestTransport;

// Bidirectional pipe for in-memory communication
// Uses a channel to preserve message order while delivering asynchronously
internal class InMemoryTransportPipe
{
    private InMemoryTransportPipe? _otherEnd;
    private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>();
    private readonly CancellationTokenSource _cts = new();

    public event Action<byte[]>? DataSent;

    public InMemoryTransportPipe()
    {
        // Start the message processing loop
        _ = ProcessMessagesAsync();
    }

    public void ConnectTo(InMemoryTransportPipe otherEnd)
    {
        _otherEnd = otherEnd;
    }

    public void Send(byte[] data)
    {
        // Queue the message for async delivery
        _otherEnd?._channel.Writer.TryWrite(data);
    }

    private async Task ProcessMessagesAsync()
    {
        try
        {
            await foreach (var data in _channel.Reader.ReadAllAsync(_cts.Token))
            {
                // Deliver message asynchronously but in order
                await Task.Run(() => DataSent?.Invoke(data), _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when disposing
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.Complete();
        _cts.Dispose();
    }
}
