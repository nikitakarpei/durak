namespace BotService.Application.PlayerIdentityServiceIntegration;

public interface IRpcClientFactory
{
    Rpc.Client.IClient Create();
}
