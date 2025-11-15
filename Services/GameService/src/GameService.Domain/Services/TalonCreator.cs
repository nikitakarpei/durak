namespace GameService.Domain;

public interface ITalonCreator
{
    public Card[] CreateTalon(GameOption[] options);
}

public class TalonCreator : ITalonCreator
{
    public Card[] CreateTalon(GameOption[] options)
    {
        var ranksOption =
            options.OfType<GameOptions.CardRanksInTalon>().FirstOrDefault() ?? throw new InvalidOperationException(
                "Card ranks in talon option is not set."
            );
        var ranks = ranksOption.Ranks;

        var nextPosition = 0;

        return Enum.GetValues<CardSuit>()
            .Order()
            .SelectMany(s =>
                ranks
                    .Order()
                    .Select(r => new RegularCard(
                        Guid.NewGuid().ToString(),
                        new CardLocations.Talon(nextPosition++),
                        s,
                        r
                    ))
                    .Cast<Card>()
            )
            .ToArray();
    }
}
