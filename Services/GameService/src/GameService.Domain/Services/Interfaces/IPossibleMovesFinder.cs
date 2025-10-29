namespace GameService.Domain;

public interface IPossibleMovesFinder
{
    public ICollection<PossibleMove> FindPossibleMoves(GameState gameState, string playerId);
}
