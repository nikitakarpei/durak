using Akka.Actor;
using Akka.DependencyInjection;

namespace GameService.Infrastructure;

public class GameCollection(IActorRefFactory actorRefFactory, IDependencyResolver dependencyResolver)
    : Application.IGameCollection
{
    public async Task CreateAsync(
        string gameId,
        Domain.GameOption[] options,
        byte[] randomSeed,
        CancellationToken cancellationToken
    )
    {
        var props = dependencyResolver.Props<GameActor>(gameId);
        actorRefFactory.ActorOf(props, GetGameActorName(gameId));

        var game = Get(gameId);
        await game.ExecuteAsync(new Domain.GameCommands.CreateGame(randomSeed, options), cancellationToken);
    }

    public void Delete(string gameId)
    {
        var selection = GetGameActorSelection(gameId);
        selection.Tell(PoisonPill.Instance);
    }

    public Application.IGame Get(string gameId)
    {
        var selection = GetGameActorSelection(gameId);
        return new GameActorGateway(selection);
    }

    private static string GetGameActorName(string gameId)
    {
        return $"game-{gameId}";
    }

    private ActorSelection GetGameActorSelection(string gameId)
    {
        return actorRefFactory.ActorSelection($"/user/{GetGameActorName(gameId)}");
    }
}
