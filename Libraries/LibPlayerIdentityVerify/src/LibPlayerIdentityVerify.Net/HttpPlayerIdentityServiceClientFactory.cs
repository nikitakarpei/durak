using Microsoft.Extensions.Logging;
using Rpc.Client;
using Rpc.Net.Transport;

namespace PlayerIdentityVerify.Net;

public sealed class HttpPlayerIdentityServiceClientFactory(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory
) : IPlayerIdentityServiceClientFactory
{
    public IClient Create(Uri serviceUrl)
    {
        HttpClient? httpClient = null;
        ClientHttpConnection? connection = null;
        RpcClient? rpcClient = null;

        try
        {
            httpClient = httpClientFactory.CreateClient("HttpPlayerIdentityServiceClient");
            connection = new ClientHttpConnection(
                loggerFactory.CreateLogger<ClientHttpConnection>(),
                Guid.NewGuid(),
                httpClient,
                serviceUrl
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

    class OwningRpcClientProxy(HttpClient httpClient, ClientHttpConnection connection, IClient rpcClient) : IClient
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

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
