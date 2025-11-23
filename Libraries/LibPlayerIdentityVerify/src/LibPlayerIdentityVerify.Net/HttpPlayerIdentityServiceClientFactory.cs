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

        return new CompositeDisposableClient(rpcClient, connection, httpClient);
    }
}
