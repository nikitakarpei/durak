using Akka.Actor;
using Akka.DependencyInjection;

namespace GameService.Infrastructure;

public class GameCollection(IActorRefFactory actorRefFactory, IDependencyResolver dependencyResolver)
    : Application.IGameCollection
{
    public void Create(string gameId, Domain.GameOption[] options)
    {
        var props = dependencyResolver.Props<GameActor>(gameId, Guid.NewGuid().GetHashCode(), options);
        actorRefFactory.ActorOf(props, GetGameActorName(gameId));
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
