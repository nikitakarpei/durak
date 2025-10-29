using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GameService.Api;

public class GetGameEventsCommandHandler(
    Application.IGameAccessTokenVerifier gameAccessTokenVerifier,
    Application.IVerifiedPlayerIdentityProvider verifiedPlayerIdentityProvider,
    EventStore.IEventStore eventStore
)
    : Rpc.Server.IStreamingCallHandler<
        Commands.GetGameEventsCommand.Types.Request,
        Commands.GetGameEventsCommand.Types.StreamResponse
    >
{
    public async IAsyncEnumerable<Commands.GetGameEventsCommand.Types.StreamResponse> HandleAsync(
        Commands.GetGameEventsCommand.Types.Request request,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var playerIdentity =
            verifiedPlayerIdentityProvider.Identity ?? throw new InvalidOperationException("Player identity not found");

        var gameAccessToken = await gameAccessTokenVerifier.VerifyAsync(request.AccessToken, cancellationToken);
        if (gameAccessToken is null)
        {
            yield return new Commands.GetGameEventsCommand.Types.StreamResponse()
            {
                InvalidAccessToken = new Commands.GetGameEventsCommand.Types.StreamResponse.Types.InvalidAccessToken(),
            };
            yield break;
        }

        Domain.GameState gameState = new();

        await using var subscription = await eventStore.SubscribeToStreamAsync(
            Application.GameEventStreams.DomainEvents(gameAccessToken.GameId),
            cancellationToken
        );

        await foreach (var @event in subscription.GetEvents(cancellationToken))
        {
            var domainEvent = DeserializeDomainEvent(@event);

            if (domainEvent is null)
            {
                throw new Exception($"Failed to deserialize domain event: {@event.Type}");
            }

            gameState = gameState.When(domainEvent);

            var apiEvent = ConvertToApiEvent(playerIdentity.PlayerId, gameState, (dynamic)domainEvent);

            var streamResponse = new Commands.GetGameEventsCommand.Types.StreamResponse();

            switch (@event)
            {
                case EventStore.ExistingRecordedEvent existingEvent:
                    streamResponse.OldEvent = new Commands.GetGameEventsCommand.Types.StreamResponse.Types.OldEvent()
                    {
                        Event = apiEvent,
                        IsLast = existingEvent.IsLast,
                    };
                    break;
                case EventStore.NewRecordedEvent newEvent:
                    streamResponse.NewEvent = new Commands.GetGameEventsCommand.Types.StreamResponse.Types.NewEvent()
                    {
                        Event = apiEvent,
                    };
                    break;
                default:
                    throw new Exception("Unknown event type");
            }

            yield return streamResponse;
        }
    }

    private object? DeserializeDomainEvent(EventStore.RecordedEvent @event)
    {
        return @event.Type switch
        {
            nameof(Domain.GameEvents.ActivePlayerChanged) =>
                JsonSerializer.Deserialize<Domain.GameEvents.ActivePlayerChanged>(@event.JsonData),
            nameof(Domain.GameEvents.AllPlayersJoined) =>
                JsonSerializer.Deserialize<Domain.GameEvents.AllPlayersJoined>(@event.JsonData),
            nameof(Domain.GameEvents.AttackCardBeaten) =>
                JsonSerializer.Deserialize<Domain.GameEvents.AttackCardBeaten>(@event.JsonData),
            nameof(Domain.GameEvents.AttackCardPlayed) =>
                JsonSerializer.Deserialize<Domain.GameEvents.AttackCardPlayed>(@event.JsonData),
            nameof(Domain.GameEvents.CardDealtToPlayer) =>
                JsonSerializer.Deserialize<Domain.GameEvents.CardDealtToPlayer>(@event.JsonData),
            nameof(Domain.GameEvents.CardDiscarded) => JsonSerializer.Deserialize<Domain.GameEvents.CardDiscarded>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.CardsInTalonShuffled) =>
                JsonSerializer.Deserialize<Domain.GameEvents.CardsInTalonShuffled>(@event.JsonData),
            nameof(Domain.GameEvents.CommonTrumpCardChosen) =>
                JsonSerializer.Deserialize<Domain.GameEvents.CommonTrumpCardChosen>(@event.JsonData),
            nameof(Domain.GameEvents.DefenceGivenUp) => JsonSerializer.Deserialize<Domain.GameEvents.DefenceGivenUp>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.FoolDeclared) => JsonSerializer.Deserialize<Domain.GameEvents.FoolDeclared>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.GameCreated) => JsonSerializer.Deserialize<Domain.GameEvents.GameCreated>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.LobbyOpened) => JsonSerializer.Deserialize<Domain.GameEvents.LobbyOpened>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.PlayerFinished) => JsonSerializer.Deserialize<Domain.GameEvents.PlayerFinished>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.PlayerJoined) => JsonSerializer.Deserialize<Domain.GameEvents.PlayerJoined>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.RoundFinished) => JsonSerializer.Deserialize<Domain.GameEvents.RoundFinished>(
                @event.JsonData
            ),
            nameof(Domain.GameEvents.RoundStarted) => JsonSerializer.Deserialize<Domain.GameEvents.RoundStarted>(
                @event.JsonData
            ),
            _ => throw new ArgumentException($"Unknown event type: {@event.Type}"),
        };
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.ActivePlayerChanged @event
    )
    {
        if (playerId == @event.PlayerId)
        {
            var yourTurnEventDto = new GameEvents.YourTurnStarted
            {
                PlayerId = @event.PlayerId,
                PossibleMoves = { @event.PossibleMoves.Select(move => move.ToApiDto()).ToArray() },
            };

            return Google.Protobuf.WellKnownTypes.Any.Pack(yourTurnEventDto);
        }
        else
        {
            var opponentTurnEventDto = new GameEvents.OpponentTurnStarted { PlayerId = @event.PlayerId };

            return Google.Protobuf.WellKnownTypes.Any.Pack(opponentTurnEventDto);
        }
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.AllPlayersJoined @event
    )
    {
        var eventDto = new GameEvents.AllPlayersJoined();

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.AttackCardBeaten @event
    )
    {
        var defenderCard = currentState.Cards.First(card => card.Id == @event.DefenderCardId);

        var eventDto = new GameEvents.AttackCardBeaten
        {
            PlayerId = @event.PlayerId,
            PlayedAt = Google.Protobuf.WellKnownTypes.TimeExtensions.ToTimestamp(@event.PlayedAt),
            AttackerCardId = @event.AttackerCardId,
            DefenderCard = Google.Protobuf.WellKnownTypes.Any.Pack(defenderCard.ToApiDto()),
        };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.AttackCardPlayed @event
    )
    {
        var playerCard = currentState.Cards.First(card => card.Id == @event.CardId);

        var eventDto = new GameEvents.AttackCardPlayed()
        {
            PlayerId = @event.PlayerId,
            PlayedAt = Google.Protobuf.WellKnownTypes.TimeExtensions.ToTimestamp(@event.PlayedAt),
            PlayedCard = Google.Protobuf.WellKnownTypes.Any.Pack(playerCard.ToApiDto()),
        };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.CardDealtToPlayer @event
    )
    {
        var dealtCard = currentState.Cards.First(card => card.Id == @event.CardId);

        if (playerId == @event.PlayerId)
        {
            var cardReceiverEventDto = new GameEvents.CardDealtToPlayer()
            {
                PlayerId = @event.PlayerId,
                DealtCard = Google.Protobuf.WellKnownTypes.Any.Pack(dealtCard.ToApiDto()),
            };

            return Google.Protobuf.WellKnownTypes.Any.Pack(cardReceiverEventDto);
        }
        else
        {
            var otherPlayersEventDto = new GameEvents.CardDealtToPlayer()
            {
                PlayerId = @event.PlayerId,
                DealtCard = Google.Protobuf.WellKnownTypes.Any.Pack(new Api.Data.UnrevealedCard { Id = dealtCard.Id }),
            };

            return Google.Protobuf.WellKnownTypes.Any.Pack(otherPlayersEventDto);
        }
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.CardDiscarded @event
    )
    {
        var eventDto = new GameEvents.CardDiscarded { CardId = @event.CardId };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.CardsInTalonShuffled @event
    )
    {
        List<GameEvents.CardsInTalonShuffled.Types.CardInTalon> cardDtos = [];

        foreach (var card in currentState.Cards)
        {
            cardDtos.Add(
                new GameEvents.CardsInTalonShuffled.Types.CardInTalon
                {
                    Id = card.Id,
                    NewPosition = ((Domain.CardLocations.Talon)card.Location).Position,
                }
            );
        }

        var eventDto = new GameEvents.CardsInTalonShuffled { Cards = { cardDtos } };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.CommonTrumpCardChosen @event
    )
    {
        var chosenCard = (Domain.RegularCard)currentState.Cards.First(card => card.Id == @event.CardId);

        var eventDto = new GameEvents.CommonTrumpCardChosen { ChosenCard = chosenCard.ToApiDto() };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.DefenceGivenUp @event
    )
    {
        var eventDto = new GameEvents.DefenceGivenUp { PlayerId = @event.PlayerId };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.FoolDeclared @event
    )
    {
        var eventDto = new GameEvents.FoolDeclared { PlayerId = @event.PlayerId };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.GameCreated @event
    )
    {
        List<Google.Protobuf.WellKnownTypes.Any> gameOptionDtos = [];

        foreach (var gameOption in @event.Options)
        {
            gameOptionDtos.Add(Google.Protobuf.WellKnownTypes.Any.Pack(gameOption.ToApiDto()));
        }

        List<GameEvents.GameCreated.Types.CardInTalon> cardDtos = [];

        foreach (var card in currentState.Cards)
        {
            cardDtos.Add(
                new GameEvents.GameCreated.Types.CardInTalon
                {
                    Id = card.Id,
                    Position = ((Domain.CardLocations.Talon)card.Location).Position,
                }
            );
        }

        var eventDto = new GameEvents.GameCreated { CardsInTalon = { cardDtos }, Options = { gameOptionDtos } };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.LobbyOpened @event
    )
    {
        var eventDto = new GameEvents.LobbyOpened();

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.PlayerFinished @event
    )
    {
        var eventDto = new GameEvents.PlayerFinished { PlayerId = @event.PlayerId };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.PlayerJoined @event
    )
    {
        var eventDto = new GameEvents.PlayerJoined { PlayerId = @event.PlayerId };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.RoundFinished @event
    )
    {
        var eventDto = new GameEvents.RoundFinished();

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        Domain.GameEvents.RoundStarted @event
    )
    {
        var eventDto = new GameEvents.RoundStarted
        {
            AttackerPlayerId = @event.AttackerId,
            DefenderPlayerId = @event.DefenderId,
        };

        return Google.Protobuf.WellKnownTypes.Any.Pack(eventDto);
    }

    private Google.Protobuf.WellKnownTypes.Any ConvertToApiEvent(
        string playerId,
        Domain.GameState currentState,
        object unknownEvent
    )
    {
        throw new NotImplementedException($"No handler for event type {unknownEvent.GetType().Name}");
    }
}
