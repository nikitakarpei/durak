namespace GameService.Domain;

public interface IPossibleMovesFinder
{
    public ICollection<PossibleMove> FindPossibleMoves(GameState gameState, string playerId);
}

public class PossibleMovesFinder(ICardMoveValidator cardMoveValidator) : IPossibleMovesFinder
{
    public ICollection<PossibleMove> FindPossibleMoves(GameState gameState, string playerId)
    {
        if (gameState.CurrentRound is null)
        {
            throw new InvalidOperationException("Round not started");
        }

        var playerCards = gameState
            .Cards.Where(card => card.Location is CardLocations.Hand hand && hand.PlayerId == playerId)
            .ToList();

        if (playerCards.Count == 0)
        {
            throw new InvalidOperationException($"Player {playerId} has no cards");
        }

        if (playerId == gameState.CurrentRound.CurrentAttackerId)
        {
            // todo: подкидной

            return playerCards.Select(card => (PossibleMove)new PossibleMoves.PlayAttackCard(card.Id)).ToList();
        }

        if (playerId == gameState.CurrentRound.CurrentDefenderId)
        {
            // todo: переводной

            var unbeatenCards = gameState
                .UnbeatenCardsOnTable()
                .OrderBy(card => ((CardLocations.Table)card.Location).PlayedAt)
                .ToList();

            if (unbeatenCards.Count == 0)
            {
                throw new InvalidOperationException("No unbeaten cards found");
            }

            var defenceCardIdsByAttackCardId = new Dictionary<string, List<string>>();

            foreach (var unbeatenCard in unbeatenCards)
            {
                var defenceCards = playerCards.Where(card =>
                    cardMoveValidator.CanCardBeBeaten(gameState, unbeatenCard, card)
                );

                defenceCardIdsByAttackCardId[unbeatenCard.Id] = defenceCards.Select(card => card.Id).ToList();
            }

            var possibleMoves = new List<PossibleMove> { new PossibleMoves.GiveUpDefence() };

            if (CanBeatAllCards(defenceCardIdsByAttackCardId))
            {
                var oldestUnbeatenCardId = unbeatenCards[0].Id;

                foreach (var defenceCardId in defenceCardIdsByAttackCardId[oldestUnbeatenCardId])
                {
                    possibleMoves.Add(new PossibleMoves.BeatAttackCard(defenceCardId, oldestUnbeatenCardId));
                }
            }

            return possibleMoves;
        }

        throw new InvalidOperationException($"Player {playerId} is neither attacker nor defender");
    }

    private static bool CanBeatAllCards(IDictionary<string, List<string>> defenceCardIdsByAttackCardId)
    {
        var usedDefenceCardIds = new HashSet<string>();
        var attackCardIds = defenceCardIdsByAttackCardId.Keys.ToList();

        return Dfs(0);

        bool Dfs(int index)
        {
            if (index == attackCardIds.Count)
                return true;

            var attackCardId = attackCardIds[index];

            foreach (var defenceCardId in defenceCardIdsByAttackCardId[attackCardId])
            {
                if (!usedDefenceCardIds.Add(defenceCardId))
                    continue;

                if (Dfs(index + 1))
                    return true;

                usedDefenceCardIds.Remove(defenceCardId);
            }

            return false;
        }
    }
}
