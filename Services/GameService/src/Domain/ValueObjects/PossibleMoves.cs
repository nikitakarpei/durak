namespace GameService.Domain;

public static class PossibleMoves
{
    public record PlayAttackCard(string YourCardId) : PossibleMove;

    public record BeatAttackCard(string YourCardId, string TargetCardId) : PossibleMove;

    public record GiveUpDefence() : PossibleMove;
}
