namespace GameService.Domain;

public abstract record GameEvent;

public static class GameEvents
{
    public record ActivePlayerChanged(string PlayerId, PossibleMove[] PossibleMoves) : GameEvent;

    public record AttackCardBeaten(string PlayerId, string AttackerCardId, string DefenderCardId, DateTime PlayedAt)
        : GameEvent;

    public record AttackCardPlayed(string PlayerId, string CardId, DateTime PlayedAt) : GameEvent;

    public record CardsDealtToPlayer(string PlayerId, string[] CardIds) : GameEvent;

    public record CardDiscarded(string CardId) : GameEvent;

    public record CardsInTalonShuffled(CardsInTalonShuffled.CardInTalon[] Cards, byte[] NewRandomSeed) : GameEvent
    {
        public record CardInTalon(string Id, int NewPosition);
    }

    public record CommonTrumpCardChosen(string CardId) : GameEvent;

    public record DefenceGivenUp(string PlayerId) : GameEvent;

    public record FoolDeclared(string PlayerId) : GameEvent;

    public record GameCreated(byte[] RandomSeed, GameOption[] Options, Card[] Cards) : GameEvent;

    public record LobbyOpened() : GameEvent;

    public record LobbyClosed() : GameEvent;

    public record PlayerJoined(string PlayerId, int Seat) : GameEvent;

    public record RoundFinished() : GameEvent;

    public record RoundStarted(string AttackerId, string DefenderId) : GameEvent;
}
