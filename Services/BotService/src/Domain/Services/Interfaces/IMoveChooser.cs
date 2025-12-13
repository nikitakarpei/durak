namespace BotService.Domain.Interfaces;

public interface IMoveChooser
{
    public PossibleMove ChooseMove(
        ICollection<PossibleMove> possibleMoves,
        ICollection<GameEventsProjection> projections
    );
}
