using BotService.Application.DomainExtensions;

namespace BotService.Application.ApiExtensions;

public static class GameEventConverters
{
    public static dynamic ConvertToDomainEvent(this Google.Protobuf.WellKnownTypes.Any @event)
    {
        return @event switch
        {
            _ when @event.Is(GameService.Api.GameEvents.GameCreated.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.GameCreated>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.CardsInTalonShuffled.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.CardsInTalonShuffled>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.CardDealtToPlayer.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.CardDealtToPlayer>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.CommonTrumpCardChosen.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.CommonTrumpCardChosen>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.RoundStarted.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.RoundStarted>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.YourTurnStarted.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.YourTurnStarted>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.OpponentTurnStarted.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.OpponentTurnStarted>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.AttackCardPlayed.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.AttackCardPlayed>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.AttackCardBeaten.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.AttackCardBeaten>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.AllPlayersJoined.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.AllPlayersJoined>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.CardDiscarded.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.CardDiscarded>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.DefenceGivenUp.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.DefenceGivenUp>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.FoolDeclared.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.FoolDeclared>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.LobbyOpened.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.LobbyOpened>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.PlayerFinished.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.PlayerFinished>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.PlayerJoined.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.PlayerJoined>()
                .ConvertToDomainEvent(),
            _ when @event.Is(GameService.Api.GameEvents.RoundFinished.Descriptor) => @event
                .Unpack<GameService.Api.GameEvents.RoundFinished>()
                .ConvertToDomainEvent(),
            _ => throw new ArgumentException($"Unknown event type: {@event.TypeUrl}"),
        };
    }

    public static Domain.GameEvents.GameCreated ConvertToDomainEvent(
        this GameService.Api.GameEvents.GameCreated gameCreated
    )
    {
        return new Domain.GameEvents.GameCreated
        {
            Options = gameCreated.Options.Select(option => option.ToDomainGameOption()).ToArray(),
            CardsInTalon = gameCreated
                .CardsInTalon.Select(card => new Domain.GameEvents.GameCreated.CardInTalon(card.Id, card.Position))
                .ToArray(),
        };
    }

    public static Domain.GameEvents.CardsInTalonShuffled ConvertToDomainEvent(
        this GameService.Api.GameEvents.CardsInTalonShuffled cardsInTalonShuffled
    )
    {
        return new Domain.GameEvents.CardsInTalonShuffled
        {
            Cards = cardsInTalonShuffled
                .Cards.Select(card => new Domain.GameEvents.CardsInTalonShuffled.CardInTalon(card.Id, card.NewPosition))
                .ToArray(),
        };
    }

    public static Domain.GameEvents.CardDealtToPlayer ConvertToDomainEvent(
        this GameService.Api.GameEvents.CardDealtToPlayer cardDealtToPlayer
    )
    {
        return new Domain.GameEvents.CardDealtToPlayer
        {
            PlayerId = cardDealtToPlayer.PlayerId,
            DealtCard = cardDealtToPlayer.DealtCard.ToDomainCardDealtToPlayerCard(),
        };
    }

    public static Domain.GameEvents.CommonTrumpCardChosen ConvertToDomainEvent(
        this GameService.Api.GameEvents.CommonTrumpCardChosen commonTrumpCardChosen
    )
    {
        return new Domain.GameEvents.CommonTrumpCardChosen
        {
            ChosenCard = new Domain.GameEvents.CommonTrumpCardChosen.RegularCard(
                commonTrumpCardChosen.ChosenCard.Id,
                commonTrumpCardChosen.ChosenCard.Suit.ToDomainCardSuit(),
                commonTrumpCardChosen.ChosenCard.Rank.ToDomainCardRank()
            ),
        };
    }

    public static Domain.GameEvents.RoundStarted ConvertToDomainEvent(
        this GameService.Api.GameEvents.RoundStarted roundStarted
    )
    {
        return new Domain.GameEvents.RoundStarted(roundStarted.AttackerPlayerId, roundStarted.DefenderPlayerId);
    }

    public static Domain.GameEvents.YourTurnStarted ConvertToDomainEvent(
        this GameService.Api.GameEvents.YourTurnStarted yourTurnStarted
    )
    {
        return new Domain.GameEvents.YourTurnStarted(
            yourTurnStarted.PlayerId,
            yourTurnStarted.PossibleMoves.Select(move => move.ToDomainPossibleMove()).ToArray()
        );
    }

    public static Domain.GameEvents.OpponentTurnStarted ConvertToDomainEvent(
        this GameService.Api.GameEvents.OpponentTurnStarted opponentTurnStarted
    )
    {
        return new Domain.GameEvents.OpponentTurnStarted(opponentTurnStarted.PlayerId);
    }

    public static Domain.GameEvents.AttackCardPlayed ConvertToDomainEvent(
        this GameService.Api.GameEvents.AttackCardPlayed attackCardPlayed
    )
    {
        return new Domain.GameEvents.AttackCardPlayed
        {
            PlayerId = attackCardPlayed.PlayerId,
            PlayedAt = attackCardPlayed.PlayedAt.ToDateTime(),
            PlayedCard = attackCardPlayed.PlayedCard.ToDomainAttackCardPlayedCard(),
        };
    }

    public static Domain.GameEvents.AttackCardBeaten ConvertToDomainEvent(
        this GameService.Api.GameEvents.AttackCardBeaten attackCardBeaten
    )
    {
        return new Domain.GameEvents.AttackCardBeaten
        {
            PlayerId = attackCardBeaten.PlayerId,
            PlayedAt = attackCardBeaten.PlayedAt.ToDateTime(),
            AttackerCardId = attackCardBeaten.AttackerCardId,
            DefenderCard = attackCardBeaten.DefenderCard.ToDomainAttackCardBeatenCard(),
        };
    }

    public static Domain.GameEvents.AllPlayersJoined ConvertToDomainEvent(
        this GameService.Api.GameEvents.AllPlayersJoined allPlayersJoined
    )
    {
        return new Domain.GameEvents.AllPlayersJoined();
    }

    public static Domain.GameEvents.CardDiscarded ConvertToDomainEvent(
        this GameService.Api.GameEvents.CardDiscarded cardDiscarded
    )
    {
        return new Domain.GameEvents.CardDiscarded(cardDiscarded.CardId);
    }

    public static Domain.GameEvents.DefenceGivenUp ConvertToDomainEvent(
        this GameService.Api.GameEvents.DefenceGivenUp defenceGivenUp
    )
    {
        return new Domain.GameEvents.DefenceGivenUp(defenceGivenUp.PlayerId);
    }

    public static Domain.GameEvents.FoolDeclared ConvertToDomainEvent(
        this GameService.Api.GameEvents.FoolDeclared foolDeclared
    )
    {
        return new Domain.GameEvents.FoolDeclared(foolDeclared.PlayerId);
    }

    public static Domain.GameEvents.LobbyOpened ConvertToDomainEvent(
        this GameService.Api.GameEvents.LobbyOpened lobbyOpened
    )
    {
        return new Domain.GameEvents.LobbyOpened();
    }

    public static Domain.GameEvents.PlayerFinished ConvertToDomainEvent(
        this GameService.Api.GameEvents.PlayerFinished playerFinished
    )
    {
        return new Domain.GameEvents.PlayerFinished(playerFinished.PlayerId);
    }

    public static Domain.GameEvents.PlayerJoined ConvertToDomainEvent(
        this GameService.Api.GameEvents.PlayerJoined playerJoined
    )
    {
        return new Domain.GameEvents.PlayerJoined(playerJoined.PlayerId, playerJoined.Seat);
    }

    public static Domain.GameEvents.RoundFinished ConvertToDomainEvent(
        this GameService.Api.GameEvents.RoundFinished roundFinished
    )
    {
        return new Domain.GameEvents.RoundFinished();
    }
}
