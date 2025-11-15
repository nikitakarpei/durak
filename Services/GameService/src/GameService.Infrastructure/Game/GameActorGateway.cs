using Akka.Actor;

namespace GameService.Infrastructure;

public class GameActorGateway(ActorSelection gameActor) : Application.IGame
{
    public void Execute(Domain.GameCommand command)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(command));
    }
}
