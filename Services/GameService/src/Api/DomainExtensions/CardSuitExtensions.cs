namespace GameService.Api;

internal static class CardSuitExtensions
{
    public static Data.CardSuit ToApiDto(this Domain.CardSuit suit)
    {
        return (Data.CardSuit)(suit);
    }
}
