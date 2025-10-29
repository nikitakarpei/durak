namespace BotService.Application.GameIntegration;

public interface IGameServiceClientFactory
{
    public Rpc.Client.IClient Create(Action onConnected, Action onDisconnected);
};
