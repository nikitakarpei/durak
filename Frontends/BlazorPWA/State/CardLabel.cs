using GameService.Api.Data;

namespace BlazorPWA.State;

public static class CardLabel
{
    public static string For(Card card) => $"{RankToText(card.Rank)}{SuitToText(card.Suit)}";

    public static string RankToText(CardRank rank) => rank switch
    {
        CardRank.Two => "2",
        CardRank.Three => "3",
        CardRank.Four => "4",
        CardRank.Five => "5",
        CardRank.Six => "6",
        CardRank.Seven => "7",
        CardRank.Eight => "8",
        CardRank.Nine => "9",
        CardRank.Ten => "10",
        CardRank.Jack => "J",
        CardRank.Queen => "Q",
        CardRank.King => "K",
        CardRank.Ace => "A",
        _ => "?"
    };

    public static string SuitToText(CardSuit suit) => suit switch
    {
        CardSuit.Clubs => "♣",
        CardSuit.Diamonds => "♦",
        CardSuit.Hearts => "♥",
        CardSuit.Spades => "♠",
        _ => "?"
    };
}
