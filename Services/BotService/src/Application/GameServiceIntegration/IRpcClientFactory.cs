namespace BotService.Application.GameServiceIntegration;

public interface IRpcClientFactory
{
    public Rpc.Client.IClient Create(string playerIdToken);
};
