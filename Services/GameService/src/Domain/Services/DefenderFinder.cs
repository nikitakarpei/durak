namespace GameService.Domain;

public interface IDefenderFinder
{
    public Player FindFirstDefender(GameState state, Player attacker);
}

public class DefenderFinder : IDefenderFinder
{
    public Player FindFirstDefender(GameState state, Player attacker)
    {
        Player? defender = null;

        for (var iteration = 0; iteration < state.Players.Count; iteration++)
        {
            defender = state.GetNextPlayerClockwise(defender?.Seat ?? attacker.Seat);

            if (state.IsPlayerHandEmpty(defender.Id) && state.IsCardTalonEmpty())
            {
                continue;
            }

            return defender;
        }

        throw new InvalidOperationException("Cannot find defender");
    }
}
