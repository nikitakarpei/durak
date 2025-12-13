using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rpc.Client;
using Rpc.Net.Transport;

namespace BotService.Infrastructure.PlayerIdentityServiceIntegration;

public sealed class RpcClientFactory(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<PlayerIdentityServiceOptions> options
) : Application.PlayerIdentityServiceIntegration.IRpcClientFactory
{
    public IClient Create()
    {
        HttpClient? httpClient = null;
        ClientHttpConnection? connection = null;
        RpcClient? rpcClient = null;

        try
        {
            httpClient = httpClientFactory.CreateClient("PlayerIdentityServiceClient");
            connection = new ClientHttpConnection(
                loggerFactory.CreateLogger<ClientHttpConnection>(),
                Guid.NewGuid(),
                httpClient,
                new Uri(options.Value.ServiceUrl)
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
