namespace BotService.Domain;

public static class GameOptions
{
    public record AmountOfPlayers(int Count) : GameOption;

    public record CardRanksInTalon(CardRank[] Ranks) : GameOption;

    public record MinHandSize(int Count) : GameOption;

    public record PlayerAfkTimerDuration(TimeSpan Duration) : GameOption;
}
