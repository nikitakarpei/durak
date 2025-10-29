namespace Rpc.Client;

public interface IPendingCall : IDisposable
{
    void SetException(Exception exception);
}
