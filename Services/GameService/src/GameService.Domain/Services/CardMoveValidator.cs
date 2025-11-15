namespace GameService.Domain;

public interface ICardMoveValidator
{
    public bool CanCardBePlayed(GameState state, Card card);

    public bool CanCardBeBeaten(GameState state, Card attackerCard, Card defenderCard);
}

public class CardMoveValidator : ICardMoveValidator
{
    public bool CanCardBePlayed(GameState state, Card card)
    {
        if (state.HasCardsOnTable())
        {
            // todo: в подкидном можно подкидывать карты того же достоинства что и на столе

            throw new NotImplementedException();
        }

        return true;
    }

    public bool CanCardBeBeaten(GameState state, Card attackerCard, Card defenderCard)
    {
        if (attackerCard is RegularCard attackerRegularCard && defenderCard is RegularCard defenderRegularCard)
        {
            if (defenderRegularCard.Suit == attackerRegularCard.Suit)
            {
                return defenderRegularCard.Rank > attackerRegularCard.Rank;
            }

            if (state.CommonTrumpSuit != null)
            {
                return defenderRegularCard.Suit == state.CommonTrumpSuit;
            }

            return false;
        }

        throw new NotImplementedException();
    }
}
