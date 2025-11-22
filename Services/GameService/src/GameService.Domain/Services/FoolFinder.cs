namespace GameService.Domain;

public interface IFoolFinder
{
    public Player? TryFindFool(GameState state);
}

public class FoolFinder : IFoolFinder
{
    public Player? TryFindFool(GameState state)
    {
        if (state.IsCardTalonEmpty())
        {
            var unfinishedPlayers = state.PlayersWithEmptyHand();

            if (unfinishedPlayers.Count() == 1)
            {
                return unfinishedPlayers.First();
            }
        }

        return null;
    }
}
