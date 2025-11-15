namespace GameService.Domain;

public abstract record GameCommand;

public static class GameCommands
{
    public record CreateGame(byte[] RandomSeed, GameOption[] Options) : GameCommand;

    public record BeatAttackCard(string PlayerId, string AttackerCardId, string DefenderCardId) : GameCommand;

    public record ConcludeAttack(string PlayerId) : GameCommand;

    public record GiveUpDefence(string PlayerId) : GameCommand;

    public record JoinLobby(string PlayerId) : GameCommand;

    public record OpenLobby() : GameCommand;

    public record CloseLobby() : GameCommand;

    public record PlayAttackCard(string PlayerId, string CardId) : GameCommand;

    public record ShuffleCardsInTalon() : GameCommand;

    public record TopUpHand(string PlayerId) : GameCommand;

    public record ChooseCommonTrumpSuit() : GameCommand;

    public record StartFirstRound() : GameCommand;

    public record StartNextRound() : GameCommand;

    public record DeclareFool(string PlayerId) : GameCommand;

    public record ChangeActivePlayer(string PlayerId) : GameCommand;

    public record FinishCurrentRound() : GameCommand;
}
