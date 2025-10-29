using Akka.Actor;

namespace GameService.Infrastructure;

public class GameActorGateway(ActorSelection gameActor) : Application.IGame
{
    public void When(Domain.GameCommands.OpenLobby openLobby)
    {
        gameActor.Tell(new GameActor.OpenLobbyMessage());
    }

    public void When(Domain.GameCommands.JoinLobby joinLobby)
    {
        gameActor.Tell(new GameActor.JoinLobbyMessage(joinLobby.PlayerId));
    }

    public void When(Domain.GameCommands.PlayAttackCard playAttackCard)
    {
        gameActor.Tell(new GameActor.PlayAttackCardMessage(playAttackCard.PlayerId, playAttackCard.CardId));
    }

    public void When(Domain.GameCommands.BeatAttackCard beatAttackCard)
    {
        gameActor.Tell(
            new GameActor.BeatAttackCardMessage(
                beatAttackCard.PlayerId,
                beatAttackCard.AttackerCardId,
                beatAttackCard.DefenderCardId
            )
        );
    }

    public void When(Domain.GameCommands.ConcludeAttack concludeAttack)
    {
        // Note: ConcludeAttack is not implemented in GameActor
        // This command is not supported in the current implementation
        throw new NotImplementedException("ConcludeAttack command is not implemented in GameActor");
    }

    public void When(Domain.GameCommands.GiveUpDefence giveUpDefence)
    {
        gameActor.Tell(new GameActor.GiveUpDefenceMessage(giveUpDefence.PlayerId));
    }
}
