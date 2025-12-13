using BotService.Domain.Interfaces;

namespace BotService.Domain;

public class LowestValueMoveChooser : IMoveChooser
{
    public PossibleMove ChooseMove(
        ICollection<PossibleMove> possibleMoves,
        ICollection<GameEventsProjection> gameEventsProjections
    )
    {
        throw new NotImplementedException();
    }
}
