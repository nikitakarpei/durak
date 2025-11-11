using Akka.Actor;

namespace GameService.Infrastructure;

public class GameActorGateway(ActorSelection gameActor) : Application.IGame
{
    public void When(Domain.GameCommands.CreateGame createGame)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(createGame));
    }

    public void When(Domain.GameCommands.OpenLobby openLobby)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(openLobby));
    }

    public void When(Domain.GameCommands.JoinLobby joinLobby)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(joinLobby));
    }

    public void When(Domain.GameCommands.PlayAttackCard playAttackCard)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(playAttackCard));
    }

    public void When(Domain.GameCommands.BeatAttackCard beatAttackCard)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(beatAttackCard));
    }

    public void When(Domain.GameCommands.ConcludeAttack concludeAttack)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(concludeAttack));
    }

    public void When(Domain.GameCommands.GiveUpDefence giveUpDefence)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(giveUpDefence));
    }

    public void When(Domain.GameCommands.StartFirstRound startFirstRound)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(startFirstRound));
    }

    public void When(Domain.GameCommands.StartNextRound startNextRound)
    {
        gameActor.Tell(new GameActor.ExecuteCommandMessage(startNextRound));
    }
}
