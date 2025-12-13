using Akka.Actor;

namespace GameService.Infrastructure;

public class GameActorGateway(ActorSelection gameActor) : Application.IGame
{
    public async Task ExecuteAsync(Domain.GameCommand command, CancellationToken cancellationToken)
    {
        await gameActor.Ask(new GameActor.ExecuteCommandMessage(command), cancellationToken);
    }
}
