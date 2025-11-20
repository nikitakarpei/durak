using System.Collections.Immutable;
using Eventuous;

namespace GameService.Domain;

public record GameState : State<GameState>
{
    public GameState()
    {
        On<GameEvents.GameCreated>(
            static (state, @event) =>
                state with
                {
                    Options = @event.Options.ToImmutableList(),
                    Cards = @event.Cards.ToImmutableHashSet(),
                    RandomSeed = @event.RandomSeed,
                    Lobby = new OpenedLobby(),
                }
        );

        On<GameEvents.PlayerJoined>(
            static (state, @event) =>
            {
                var newState = state with { Players = state.Players.Add(new Player(@event.PlayerId, @event.Seat)) };

                if (newState.Players.Count == state.Options.OfType<GameOptions.AmountOfPlayers>().First().Count)
                {
                    newState = newState with { Lobby = new LobbyFilled() };
                }

                return newState;
            }
        );

        On<GameEvents.CardsInTalonShuffled>(
            static (state, @event) =>
            {
                var newState = state with { RandomSeed = @event.NewRandomSeed };

                newState = @event.Cards.Aggregate(
                    newState,
                    (current, cardDto) =>
                    {
                        var oldCard = current.Cards.First(card => card.Id == cardDto.Id);
                        var newCard = oldCard with { Location = new CardLocations.Talon(cardDto.NewPosition) };

                        return current with
                        {
                            Cards = current.Cards.Remove(oldCard).Add(newCard),
                        };
                    }
                );

                return newState;
            }
        );

        On<GameEvents.CardsDealtToPlayer>(
            static (state, @event) =>
            {
                var newState = state with { LastDealtCardId = @event.CardIds.Last() };

                foreach (var cardId in @event.CardIds)
                {
                    var oldCard = state.Cards.First(card => card.Id == cardId);
                    var newCard = oldCard with { Location = new CardLocations.Hand(@event.PlayerId) };

                    newState = newState with { Cards = newState.Cards.Remove(oldCard).Add(newCard) };
                }

                return newState;
            }
        );

        On<GameEvents.CommonTrumpCardChosen>(
            static (state, @event) =>
            {
                var regularCard = (RegularCard)state.Cards.First(card => card.Id == @event.CardId);

                return state with
                {
                    CommonTrumpSuit = regularCard.Suit,
                };
            }
        );

        On<GameEvents.RoundStarted>(
            static (state, @event) =>
                state with
                {
                    CurrentRound = new Round(
                        @event.RoundNumber,
                        @event.AttackerId,
                        @event.DefenderId,
                        @event.AttackerId,
                        @event.DefenderId,
                        @event.AttackerId,
                        false
                    ),
                }
        );

        On<GameEvents.ActivePlayerChanged>(
            static (state, @event) =>
                state with
                {
                    CurrentRound = state.CurrentRound! with { ActivePlayerId = @event.PlayerId },
                }
        );

        On<GameEvents.AttackCardPlayed>(
            static (state, @event) =>
            {
                var oldCard = state.Cards.First(card => card.Id == @event.CardId);
                var newCard = oldCard with
                {
                    Location = new CardLocations.Table(@event.PlayedAt, new CardRolesOnTable.Attack()),
                };

                return state with
                {
                    Cards = state.Cards.Remove(oldCard).Add(newCard),
                };
            }
        );

        On<GameEvents.AttackCardBeaten>(
            static (state, @event) =>
            {
                var oldCard = state.Cards.First(card => card.Id == @event.DefenderCardId);
                var newCard = oldCard with
                {
                    Location = new CardLocations.Table(
                        @event.PlayedAt,
                        new CardRolesOnTable.Defence(@event.AttackerCardId)
                    ),
                };

                return state with
                {
                    Cards = state.Cards.Remove(oldCard).Add(newCard),
                };
            }
        );

        On<GameEvents.DefenceGivenUp>(
            static (state, @event) => state with { CurrentRound = state.CurrentRound! with { DefenceGivenUp = true } }
        );

        On<GameEvents.RoundFinished>(
            static (state, @event) =>
                state with
                {
                    PlayedRounds = state.PlayedRounds.Add(state.CurrentRound!),
                    CurrentRound = null,
                }
        );

        On<GameEvents.CardsOnTableDiscarded>(
            static (state, @event) =>
            {
                var newState = state;

                foreach (var cardId in @event.CardIds)
                {
                    var oldCard = state.Cards.First(card => card.Id == cardId);
                    var newCard = oldCard with { Location = new CardLocations.DiscardPile() };

                    newState = newState with { Cards = newState.Cards.Remove(oldCard).Add(newCard) };
                }

                return newState;
            }
        );

        On<GameEvents.FoolDeclared>(
            static (state, @event) =>
                state with
                {
                    FoolId = @event.PlayerId,
                    PlayedRounds = state.PlayedRounds.Add(state.CurrentRound!),
                    CurrentRound = null,
                }
        );
    }

    public ImmutableHashSet<Player> Players { get; init; } = ImmutableHashSet<Player>.Empty;
    public ImmutableHashSet<Card> Cards { get; init; } = ImmutableHashSet<Card>.Empty;
    public ImmutableList<GameOption> Options { get; init; } = ImmutableList<GameOption>.Empty;
    public ImmutableList<Round> PlayedRounds { get; init; } = ImmutableList<Round>.Empty;
    public Lobby? Lobby { get; init; }
    public string? LastDealtCardId { get; init; }
    public CardSuit? CommonTrumpSuit { get; init; }
    public Round? CurrentRound { get; init; }
    public string? FoolId { get; init; }
    public byte[] RandomSeed { get; init; } = Array.Empty<byte>();

    public bool HasCardsOnTable()
    {
        return Cards.Any(card => card.Location is CardLocations.Table);
    }

    public bool IsPlayerHandEmpty(string playerId)
    {
        return !Cards.Any(card => card.Location is CardLocations.Hand hand && hand.PlayerId == playerId);
    }

    public bool IsCardTalonEmpty()
    {
        return !Cards.Any(card => card.Location is CardLocations.Talon);
    }

    public bool AreAllCardsBeaten()
    {
        return Cards
            .Where(card => card.Location is CardLocations.Table { Role: CardRolesOnTable.Attack })
            .Select(card => card.Id)
            .All(IsCardBeaten);
    }

    private bool IsCardBeaten(string cardId)
    {
        return Cards.Any(card =>
            card.Location is CardLocations.Table { Role: CardRolesOnTable.Defence defence }
            && defence.BeatenCardId == cardId
        );
    }

    public Card? GetBottomCardFromTalon()
    {
        return Cards
            .Where(card => card is RegularCard && card.Location is CardLocations.Talon)
            .OrderBy(card => ((CardLocations.Talon)card.Location).Position)
            .FirstOrDefault();
    }

    public Card GetLastDealtCard()
    {
        if (LastDealtCardId == null)
        {
            throw new InvalidOperationException("No last dealt card found");
        }

        return Cards.First(card => card.Id == LastDealtCardId);
    }

    private int GetNextSeatClockwise(int seat) => (seat + 1) % Players.Count;

    public Player GetNextPlayerClockwise(int seat)
    {
        return Players.First(player => player.Seat == GetNextSeatClockwise(seat));
    }

    public IEnumerable<Card> CardsOnTable()
    {
        return Cards.Where(card => card.Location is CardLocations.Table);
    }

    public IEnumerable<Card> CardsOnTableByPlayOrder()
    {
        return Cards
            .Where(card => card.Location is CardLocations.Table)
            .OrderBy(card => ((CardLocations.Table)card.Location).PlayedAt);
    }

    public IEnumerable<Card> UnbeatenCardsOnTable()
    {
        return Cards.Where(attackCard =>
            attackCard.Location is CardLocations.Table { Role: CardRolesOnTable.Attack }
            && !Cards.Any(defenceCard =>
                defenceCard.Location is CardLocations.Table { Role: CardRolesOnTable.Defence defence }
                && defence.BeatenCardId == attackCard.Id
            )
        );
    }

    public IEnumerable<Player> PlayersWithEmptyHand()
    {
        return Players.Where(player => IsPlayerHandEmpty(player.Id));
    }
}
