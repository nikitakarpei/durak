namespace GameService.Api;

internal static class CardExtensions
{
    public static Google.Protobuf.IMessage ToApiDto(this Domain.Card card)
    {
        return card switch
        {
            Domain.RegularCard regularCard => regularCard.ToApiDto(),
            Domain.JokerCard jokerCard => jokerCard.ToApiDto(),
            _ => throw new Exception($"Unknown card type: {card.GetType()}"),
        };
    }

    public static Data.RegularCard ToApiDto(this Domain.RegularCard card)
    {
        return new Data.RegularCard
        {
            Id = card.Id,
            Suit = card.Suit.ToApiDto(),
            Rank = card.Rank.ToApiDto(),
        };
    }

    public static Data.JokerCard ToApiDto(this Domain.JokerCard card)
    {
        return new Data.JokerCard { Id = card.Id, Color = (Data.JokerColor)(card.Color) };
    }
}
