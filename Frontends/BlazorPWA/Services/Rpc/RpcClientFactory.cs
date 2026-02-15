using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Rpc.Client;
using Rpc.Net.Transport;
using Rpc.Transport;

namespace BlazorPWA.Services.Rpc;

public sealed class RpcClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public RpcClientFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public IClient Create(Uri endpoint)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var connectionLogger = _loggerFactory.CreateLogger<ClientHttpConnection>();
        var connection = new ClientHttpConnection(connectionLogger, Guid.NewGuid(), httpClient, endpoint);
        var clientLogger = _loggerFactory.CreateLogger<RpcClient>();
        return new RpcClient(connection, clientLogger);
    }
}
