namespace GameService.Application;

public interface IGame
{
    public void When(Domain.GameCommands.CreateGame createGame);
    public void When(Domain.GameCommands.OpenLobby openLobby);
    public void When(Domain.GameCommands.JoinLobby joinLobby);
    public void When(Domain.GameCommands.PlayAttackCard playAttackCard);
    public void When(Domain.GameCommands.BeatAttackCard beatAttackCard);
    public void When(Domain.GameCommands.ConcludeAttack concludeAttack);
    public void When(Domain.GameCommands.GiveUpDefence giveUpDefence);
    public void When(Domain.GameCommands.StartFirstRound startFirstRound);
    public void When(Domain.GameCommands.StartNextRound startNextRound);
}
