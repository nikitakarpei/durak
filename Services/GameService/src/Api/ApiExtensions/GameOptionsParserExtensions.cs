namespace GameService.Api;

internal static class GameOptionsParserExtensions
{
    public static Domain.GameOption[] ParseGameOptions(this Commands.CreateGameCommand.Types.Request request)
    {
        return request.Options.Select(ParseGameOption).ToArray();
    }

    public static Domain.GameOption ParseGameOption(this Google.Protobuf.WellKnownTypes.Any apiOption)
    {
        if (apiOption.Is(Data.GameOptions.AmountOfPlayers.Descriptor))
        {
            var amountOfPlayers = apiOption.Unpack<Data.GameOptions.AmountOfPlayers>();
            return new Domain.GameOptions.AmountOfPlayers(amountOfPlayers.Count);
        }

        if (apiOption.Is(Data.GameOptions.CardRanksInTalon.Descriptor))
        {
            var cardRanksInTalon = apiOption.Unpack<Data.GameOptions.CardRanksInTalon>();
            var ranks = cardRanksInTalon.Ranks.Select(rank => (Domain.CardRank)(rank + 1)).ToArray();
            return new Domain.GameOptions.CardRanksInTalon(ranks);
        }

        if (apiOption.Is(Data.GameOptions.MinHandSize.Descriptor))
        {
            var minHandSize = apiOption.Unpack<Data.GameOptions.MinHandSize>();
            return new Domain.GameOptions.MinHandSize(minHandSize.Count);
        }

        if (apiOption.Is(Data.GameOptions.PlayerAfkTimerDuration.Descriptor))
        {
            var playerAfkTimerDuration = apiOption.Unpack<Data.GameOptions.PlayerAfkTimerDuration>();
            return new Domain.GameOptions.PlayerAfkTimerDuration(playerAfkTimerDuration.Duration.ToTimeSpan());
        }

        throw new ArgumentException($"Unknown game option type: {apiOption.TypeUrl}");
    }
}
