using BotService.Domain.Interfaces;

namespace BotService.Domain;

public class RandomMoveChooser(Random random) : IMoveChooser
{
    public PossibleMove ChooseMove(
        ICollection<PossibleMove> possibleMoves,
        ICollection<GameEventsProjection> projections
    )
    {
        return possibleMoves.ElementAt(random.Next(0, possibleMoves.Count));
    }
}
