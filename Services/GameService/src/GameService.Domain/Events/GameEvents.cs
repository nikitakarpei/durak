namespace GameService.Domain;

public static class GameEvents
{
    public record ActivePlayerChanged(string PlayerId, PossibleMove[] PossibleMoves);

    public record AllPlayersJoined();

    public record AttackCardBeaten(string PlayerId, string AttackerCardId, string DefenderCardId, DateTime PlayedAt);

    public record AttackCardPlayed(string PlayerId, string CardId, DateTime PlayedAt);

    public record CardDealtToPlayer(string PlayerId, string CardId);

    public record CardDiscarded(string CardId);

    public record CardsInTalonShuffled(CardsInTalonShuffled.CardInTalon[] Cards)
    {
        public record CardInTalon(string Id, int NewPosition);
    }

    public record CommonTrumpCardChosen(string CardId);

    public record DefenceGivenUp(string PlayerId);

    public record FoolDeclared(string PlayerId);

    public record GameCreated(GameOption[] Options, Card[] Cards);

    public record LobbyOpened();

    public record PlayerFinished(string PlayerId);

    public record PlayerJoined(string PlayerId, int Seat);

    public record RoundFinished();

    public record RoundStarted(string AttackerId, string DefenderId);
}
