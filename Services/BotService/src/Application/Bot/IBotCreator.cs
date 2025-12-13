namespace BotService.Application.Bot;

public interface IBotCreator
{
    void Create(
        Rpc.Client.IClient gameClient,
        Domain.Interfaces.IGameEventsProjector<Domain.GameState> gameStateProjector,
        Domain.Interfaces.IMoveChooser moveChooser,
        string gameId,
        string playerId
    );
}
