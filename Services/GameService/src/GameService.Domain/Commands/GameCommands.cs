namespace GameService.Domain;

public static class GameCommands
{
    public record struct CreateGame(byte[] RandomSeed, GameOption[] Options);

    public record struct BeatAttackCard(string PlayerId, string AttackerCardId, string DefenderCardId);

    public record struct ConcludeAttack(string PlayerId);

    public record struct GiveUpDefence(string PlayerId);

    public record struct JoinLobby(string PlayerId);

    public record struct OpenLobby();

    public record struct PlayAttackCard(string PlayerId, string CardId);

    public record struct StartFirstRound();

    public record struct StartNextRound();
}
