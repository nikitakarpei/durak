namespace GameService.Domain;

public static class GameEvents
{
    public record ActivePlayerChanged(
        string PlayerId,
        PossibleMove[] PossibleMoves,
        TimeSpan PlayerAfkTimerDuration,
        int? CurrentPlayerAfkTimerGeneration,
        int NextPlayerAfkTimerGeneration
    );

    public record AttackCardBeaten(string PlayerId, string AttackerCardId, string DefenderCardId, DateTime PlayedAt);

    public record AttackCardPlayed(string PlayerId, string CardId, DateTime PlayedAt);

    public record CardsDealtToPlayer(string PlayerId, string[] CardIds);

    public record CardsOnTableDiscarded(string[] CardIds);

    public record CardsInTalonShuffled(CardsInTalonShuffled.CardInTalon[] Cards, byte[] NewRandomSeed)
    {
        public record CardInTalon(string Id, int NewPosition);
    }

    public record CommonTrumpCardChosen(string CardId);

    public record DefenceGivenUp(string PlayerId);

    public record FoolDeclared(string PlayerId);

    public record GameCreated(byte[] RandomSeed, GameOption[] Options, Card[] Cards);

    public record PlayerJoined(string PlayerId, int Seat, bool IsLastPlayer);

    public record PlayerTookCardsFromTable(string PlayerId, string[] CardIds);

    public record RoundFinished(int RoundNumber, int PlayerAfkTimerGeneration);

    public record RoundStarted(int RoundNumber, string AttackerId, string DefenderId);
}
