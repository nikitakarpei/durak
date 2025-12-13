using Akka.Actor;

namespace BotService.Infrastructure.Bot;

public class BotCreator(IActorRefFactory actorRefFactory) : Application.Bot.IBotCreator
{
    public void Create(
        Rpc.Client.IClient gameClient,
        Domain.Interfaces.IGameEventsProjector<Domain.GameState> gameStateProjector,
        Domain.Interfaces.IMoveChooser moveChooser,
        string gameId,
        string playerId
    )
    {
        var bot = actorRefFactory.ActorOf(
            Props.Create<BotActor>(gameClient, gameStateProjector, moveChooser, gameId, playerId),
            GetBotActorName(gameId, playerId)
        );
        bot.Tell(new BotActor.EnterGameMessage());
    }

    private static string GetBotActorName(string gameId, string playerId)
    {
        return $"bot-{gameId}-{playerId}";
    }
}
