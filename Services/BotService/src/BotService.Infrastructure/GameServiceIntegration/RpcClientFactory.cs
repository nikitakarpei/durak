using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rpc.Client;
using Rpc.Net.Transport;

namespace BotService.Infrastructure.GameServiceIntegration;

public sealed class RpcClientFactory(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<GameServiceOptions> options
) : Application.GameServiceIntegration.IRpcClientFactory
{
    public IClient Create(string playerIdToken)
    {
        HttpClient? httpClient = null;
        ClientHttpConnection? connection = null;
        RpcClient? rpcClient = null;

        try
        {
            var uri = new Uri($"{options.Value.ServiceUrl}?player_id_token={Uri.EscapeDataString(playerIdToken)}");

            httpClient = httpClientFactory.CreateClient("GameServiceClient");
            connection = new ClientHttpConnection(
                loggerFactory.CreateLogger<ClientHttpConnection>(),
                Guid.NewGuid(),
                httpClient,
                uri
            );
            rpcClient = new RpcClient(connection, loggerFactory.CreateLogger<RpcClient>());
        }
        catch
        {
            httpClient?.Dispose();
            connection?.Dispose();
            rpcClient?.Dispose();
            throw;
        }

        return new OwningRpcClientProxy(httpClient, connection, rpcClient);
    }

    private class OwningRpcClientProxy(HttpClient httpClient, ClientHttpConnection connection, IClient rpcClient)
        : IClient
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            rpcClient.Dispose();
            connection.Dispose();
            httpClient.Dispose();

            GC.SuppressFinalize(this);
        }

        Task<TResponse> IClient.UnaryCall<TRequest, TResponse>(
            string method,
            TRequest request,
            CancellationToken cancellationToken
        )
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(OwningRpcClientProxy));
            return rpcClient.UnaryCall<TRequest, TResponse>(method, request, cancellationToken);
        }

        IAsyncEnumerable<TResponse> IClient.StreamingCall<TRequest, TResponse>(
            string method,
            TRequest request,
            CancellationToken cancellationToken
        )
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(OwningRpcClientProxy));
            return rpcClient.StreamingCall<TRequest, TResponse>(method, request, cancellationToken);
        }
    }
}
