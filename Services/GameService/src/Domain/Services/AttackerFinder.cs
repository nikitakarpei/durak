namespace GameService.Domain;

public interface IAttackerFinder
{
    public Player FindFirstAttackerForFirstRound(GameState state);

    public Player FindFirstAttackerForNextRound(GameState state, Round previousRound);
}

public class AttackerFinder : IAttackerFinder
{
    public Player FindFirstAttackerForFirstRound(GameState state)
    {
        // todo: первый атакующий должен выбираться по младшему козырю

        return state.Players.First(player => player.Seat == 0);
    }

    public Player FindFirstAttackerForNextRound(GameState state, Round previousRound)
    {
        Player? nextAttacker = null;

        for (var iteration = 0; iteration < state.Players.Count; iteration++)
        {
            if (nextAttacker == null)
            {
                nextAttacker = state.Players.First(player => player.Id == previousRound.CurrentDefenderId);

                if (previousRound.DefenceGivenUp)
                {
                    continue;
                }
            }
            else
            {
                nextAttacker = state.GetNextPlayerClockwise(nextAttacker.Seat);
            }

            if (state.IsPlayerHandEmpty(nextAttacker.Id) && state.IsCardTalonEmpty())
            {
                continue;
            }

            return nextAttacker;
        }

        throw new InvalidOperationException("Cannot find attacker for next round");
    }
}
