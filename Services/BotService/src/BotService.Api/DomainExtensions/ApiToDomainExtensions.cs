namespace BotService.Api.DomainExtensions;

public static class ApiToDomainExtensions
{
    public static Domain.CardSuit ToDomainCardSuit(this GameService.Api.Data.CardSuit apiSuit)
    {
        return (Domain.CardSuit)(apiSuit);
    }

    public static Domain.CardRank ToDomainCardRank(this GameService.Api.Data.CardRank apiRank)
    {
        return (Domain.CardRank)(apiRank + 1);
    }

    public static Domain.JokerColor ToDomainJokerColor(this GameService.Api.Data.JokerColor apiColor)
    {
        return (Domain.JokerColor)(apiColor);
    }

    public static Domain.GameOption ToDomainGameOption(this Google.Protobuf.WellKnownTypes.Any apiOption)
    {
        if (apiOption.Is(GameService.Api.Data.GameOptions.AmountOfPlayers.Descriptor))
        {
            var amountOfPlayers = apiOption.Unpack<GameService.Api.Data.GameOptions.AmountOfPlayers>();
            return new Domain.GameOptions.AmountOfPlayers(amountOfPlayers.Count);
        }

        if (apiOption.Is(GameService.Api.Data.GameOptions.CardRanksInTalon.Descriptor))
        {
            var cardRanksInTalon = apiOption.Unpack<GameService.Api.Data.GameOptions.CardRanksInTalon>();
            var ranks = cardRanksInTalon.Ranks.Select(rank => (Domain.CardRank)(rank + 1)).ToArray();
            return new Domain.GameOptions.CardRanksInTalon(ranks);
        }

        if (apiOption.Is(GameService.Api.Data.GameOptions.MinHandSize.Descriptor))
        {
            var minHandSize = apiOption.Unpack<GameService.Api.Data.GameOptions.MinHandSize>();
            return new Domain.GameOptions.MinHandSize(minHandSize.Count);
        }

        throw new ArgumentException($"Unknown game option type: {apiOption.TypeUrl}");
    }

    public static Domain.GameEvents.CardsDealtToPlayer.Card ToDomainCardsDealtToPlayerCard(
        this Google.Protobuf.WellKnownTypes.Any apiCard
    )
    {
        if (apiCard.Is(GameService.Api.Data.RegularCard.Descriptor))
        {
            var regularCard = apiCard.Unpack<GameService.Api.Data.RegularCard>();
            return new Domain.GameEvents.CardsDealtToPlayer.RegularCard(
                regularCard.Id,
                regularCard.Suit.ToDomainCardSuit(),
                regularCard.Rank.ToDomainCardRank()
            );
        }

        if (apiCard.Is(GameService.Api.Data.JokerCard.Descriptor))
        {
            var jokerCard = apiCard.Unpack<GameService.Api.Data.JokerCard>();
            return new Domain.GameEvents.CardsDealtToPlayer.JokerCard(
                jokerCard.Id,
                jokerCard.Color.ToDomainJokerColor()
            );
        }

        if (apiCard.Is(GameService.Api.Data.UnrevealedCard.Descriptor))
        {
            var unrevealedCard = apiCard.Unpack<GameService.Api.Data.UnrevealedCard>();
            return new Domain.GameEvents.CardsDealtToPlayer.UnrevealedCard(unrevealedCard.Id);
        }

        throw new ArgumentException($"Unknown card type: {apiCard.TypeUrl}");
    }

    public static Domain.GameEvents.AttackCardPlayed.Card ToDomainAttackCardPlayedCard(
        this Google.Protobuf.WellKnownTypes.Any apiCard
    )
    {
        if (apiCard.Is(GameService.Api.Data.RegularCard.Descriptor))
        {
            var regularCard = apiCard.Unpack<GameService.Api.Data.RegularCard>();
            return new Domain.GameEvents.AttackCardPlayed.RegularCard(
                regularCard.Id,
                regularCard.Suit.ToDomainCardSuit(),
                regularCard.Rank.ToDomainCardRank()
            );
        }

        if (apiCard.Is(GameService.Api.Data.JokerCard.Descriptor))
        {
            var jokerCard = apiCard.Unpack<GameService.Api.Data.JokerCard>();
            return new Domain.GameEvents.AttackCardPlayed.JokerCard(jokerCard.Id, jokerCard.Color.ToDomainJokerColor());
        }

        throw new ArgumentException($"Unknown card type: {apiCard.TypeUrl}");
    }

    public static Domain.GameEvents.AttackCardBeaten.Card ToDomainAttackCardBeatenCard(
        this Google.Protobuf.WellKnownTypes.Any apiCard
    )
    {
        if (apiCard.Is(GameService.Api.Data.RegularCard.Descriptor))
        {
            var regularCard = apiCard.Unpack<GameService.Api.Data.RegularCard>();
            return new Domain.GameEvents.AttackCardBeaten.RegularCard(
                regularCard.Id,
                regularCard.Suit.ToDomainCardSuit(),
                regularCard.Rank.ToDomainCardRank()
            );
        }

        if (apiCard.Is(GameService.Api.Data.JokerCard.Descriptor))
        {
            var jokerCard = apiCard.Unpack<GameService.Api.Data.JokerCard>();
            return new Domain.GameEvents.AttackCardBeaten.JokerCard(jokerCard.Id, jokerCard.Color.ToDomainJokerColor());
        }

        throw new ArgumentException($"Unknown card type: {apiCard.TypeUrl}");
    }

    public static Domain.PossibleMove ToDomainPossibleMove(this Google.Protobuf.WellKnownTypes.Any apiPossibleMove)
    {
        if (apiPossibleMove.Is(GameService.Api.PossibleMoves.PlayAttackCard.Descriptor))
        {
            var playAttackCard = apiPossibleMove.Unpack<GameService.Api.PossibleMoves.PlayAttackCard>();
            return new Domain.PossibleMoves.PlayAttackCard(playAttackCard.YourCardId);
        }

        if (apiPossibleMove.Is(GameService.Api.PossibleMoves.BeatAttackCard.Descriptor))
        {
            var beatAttackCard = apiPossibleMove.Unpack<GameService.Api.PossibleMoves.BeatAttackCard>();
            return new Domain.PossibleMoves.BeatAttackCard(beatAttackCard.YourCardId, beatAttackCard.TargetCardId);
        }

        if (apiPossibleMove.Is(GameService.Api.PossibleMoves.GiveUpDefence.Descriptor))
        {
            return new Domain.PossibleMoves.GiveUpDefence();
        }

        throw new ArgumentException($"Unknown possible move type: {apiPossibleMove.TypeUrl}");
    }
}

