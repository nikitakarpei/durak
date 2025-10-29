using System.Collections.Immutable;

namespace BotService.Domain;

public record GameState : GameEventsProjection
{
    public ImmutableHashSet<Card> Cards { get; init; } = ImmutableHashSet<Card>.Empty;
    public ImmutableList<GameOption> Options { get; init; } = ImmutableList<GameOption>.Empty;
    public CardSuit? CommonTrumpSuit { get; init; }
    public Round? CurrentRound { get; init; }

    public IEnumerable<Card> UnbeatenCardsOnTable()
    {
        return Cards.Where(attackCard =>
            attackCard.Location is CardLocations.Table { Role: CardRolesOnTable.Attack }
            && !Cards.Any(defenceCard =>
                defenceCard.Location is CardLocations.Table { Role: CardRolesOnTable.Defence defence }
                && defence.BeatenCardId != attackCard.Id
            )
        );
    }
}
