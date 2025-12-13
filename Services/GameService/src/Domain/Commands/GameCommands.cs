namespace GameService.Domain;

public abstract record GameCommand;

public static class GameCommands
{
    public record CreateGame(byte[] RandomSeed, GameOption[] Options) : GameCommand;

    public record BeatAttackCard(string PlayerId, string AttackerCardId, string DefenderCardId) : GameCommand;

    public record ConcludeAttack(string PlayerId) : GameCommand;

    public record GiveUpDefence(string PlayerId) : GameCommand;

    public record JoinLobby(string PlayerId) : GameCommand;

    public record PlayAttackCard(string PlayerId, string CardId) : GameCommand;

    public record HandlePlayerAfkTimerExpired(int TimerGeneration) : GameCommand;
}
