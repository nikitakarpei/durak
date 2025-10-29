namespace Rpc.Client;

public interface IUnaryPendingCall : IPendingCall
{
    void SetResponse(Google.Protobuf.WellKnownTypes.Any packedResponse);
}

internal class PendingUnaryCall<TResponse> : IUnaryPendingCall
    where TResponse : class, Google.Protobuf.IMessage, new()
{
    private readonly TaskCompletionSource<TResponse> _promise;
    private readonly CancellationTokenRegistration _cancellationRegistration;

    public PendingUnaryCall(
        TaskCompletionSource<TResponse> promise,
        CancellationTokenRegistration cancellationRegistration
    )
    {
        _promise = promise;
        _cancellationRegistration = cancellationRegistration;
    }

    public void SetResponse(Google.Protobuf.WellKnownTypes.Any packedResponse)
    {
        try
        {
            var response = packedResponse.Unpack<TResponse>();
            _promise.SetResult(response);
        }
        catch (Exception ex)
        {
            _promise.SetException(new RpcException("Failed to unpack response", ex));
        }
        finally
        {
            _cancellationRegistration.Unregister();
        }
    }

    public void SetException(Exception exception)
    {
        _promise.TrySetException(exception);
        _cancellationRegistration.Unregister();
    }

    public void Dispose()
    {
        _cancellationRegistration.Dispose();
    }
}
