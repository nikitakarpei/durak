namespace BotService.Domain;

public static class GameEvents
{
    public record struct YourTurnStarted(string PlayerId, PossibleMove[] PossibleMoves);

    public record struct OpponentTurnStarted(string PlayerId);

    public record struct AttackCardBeaten
    {
        public abstract record Card(string Id);

        public record RegularCard(string Id, CardSuit Suit, CardRank Rank) : Card(Id);

        public record JokerCard(string Id, JokerColor Color) : Card(Id);

        public string PlayerId { get; init; }
        public DateTime PlayedAt { get; init; }
        public string AttackerCardId { get; init; }
        public Card DefenderCard { get; init; }
    }

    public record struct AttackCardPlayed
    {
        public abstract record Card(string Id);

        public record RegularCard(string Id, CardSuit Suit, CardRank Rank) : Card(Id);

        public record JokerCard(string Id, JokerColor Color) : Card(Id);

        public string PlayerId { get; init; }
        public DateTime PlayedAt { get; init; }
        public Card PlayedCard { get; init; }
    }

    public record struct CardsDealtToPlayer
    {
        public abstract record Card(string Id);

        public record UnrevealedCard(string Id) : Card(Id);

        public record RegularCard(string Id, CardSuit Suit, CardRank Rank) : Card(Id);

        public record JokerCard(string Id, JokerColor Color) : Card(Id);

        public string PlayerId { get; init; }
        public Card[] DealtCards { get; init; }
    }

    public record struct CardsOnTableDiscarded(string[] CardIds);

    public record struct CardsInTalonShuffled
    {
        public record struct CardInTalon(string Id, int NewPosition);

        public CardInTalon[] Cards { get; init; }
    }

    public record struct CommonTrumpCardChosen
    {
        public record RegularCard(string Id, CardSuit Suit, CardRank Rank);

        public RegularCard ChosenCard { get; init; }
    }

    public record struct DefenceGivenUp(string PlayerId);

    public record struct FoolDeclared(string PlayerId);

    public record struct GameCreated
    {
        public record struct CardInTalon(string Id, int Position);

        public GameOption[] Options { get; init; }
        public CardInTalon[] CardsInTalon { get; init; }
    }

    public record struct PlayerJoined(string PlayerId, int Seat);

    public record struct PlayerTookCardsFromTable(string PlayerId, string[] CardIds);

    public record struct RoundFinished;

    public record struct RoundStarted(string AttackerPlayerId, string DefenderPlayerId);
}
