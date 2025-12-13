namespace GameService.Api;

internal static class CardRankExtensions
{
    public static Data.CardRank ToApiDto(this Domain.CardRank rank)
    {
        return (Data.CardRank)(rank - 1);
    }
}
