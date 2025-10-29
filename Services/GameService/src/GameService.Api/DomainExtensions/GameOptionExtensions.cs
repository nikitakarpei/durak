namespace GameService.Api;

internal static class GameOptionExtensions
{
    public static Google.Protobuf.IMessage ToApiDto(this Domain.GameOption gameOption)
    {
        return gameOption switch
        {
            Domain.GameOptions.AmountOfPlayers amountOfPlayers => amountOfPlayers.ToApiDto(),
            Domain.GameOptions.CardRanksInTalon cardRanksInTalon => cardRanksInTalon.ToApiDto(),
            Domain.GameOptions.MinHandSize minHandSize => minHandSize.ToApiDto(),
            _ => throw new Exception($"Unknown game option type: {gameOption.GetType()}"),
        };
    }

    public static Data.GameOptions.AmountOfPlayers ToApiDto(this Domain.GameOptions.AmountOfPlayers amountOfPlayers)
    {
        return new Data.GameOptions.AmountOfPlayers { Count = amountOfPlayers.Count };
    }

    public static Data.GameOptions.CardRanksInTalon ToApiDto(this Domain.GameOptions.CardRanksInTalon cardRanksInTalon)
    {
        return new Data.GameOptions.CardRanksInTalon
        {
            Ranks = { cardRanksInTalon.Ranks.Select(rank => (Data.CardRank)(rank - 1)) },
        };
    }

    public static Data.GameOptions.MinHandSize ToApiDto(this Domain.GameOptions.MinHandSize minHandSize)
    {
        return new Data.GameOptions.MinHandSize { Count = minHandSize.Count };
    }
}
