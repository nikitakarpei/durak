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
                }
        );

        On<GameEvents.LobbyOpened>(static (state, @event) => state with { Lobby = new Lobby() });

        On<GameEvents.PlayerJoined>(
            static (state, @event) =>
                state with
                {
                    Players = state.Players.Add(new Player(@event.PlayerId, @event.Seat, false)),
                }
        );

        On<GameEvents.AllPlayersJoined>(static (state, @event) => state with { Lobby = null });

        On<GameEvents.CardsInTalonShuffled>(
            static (state, @event) =>
            {
                return Enumerable.Aggregate(
                    @event.Cards,
                    state,
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
            }
        );

        On<GameEvents.CardDealtToPlayer>(
            static (state, @event) =>
            {
                var oldCard = state.Cards.First(card => card.Id == @event.CardId);
                var newCard = oldCard with { Location = new CardLocations.Hand(@event.PlayerId) };

                return state with
                {
                    Cards = state.Cards.Remove(oldCard).Add(newCard),
                    LastDealtCardId = oldCard.Id,
                };
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

        On<GameEvents.PlayerFinished>(
            static (state, @event) =>
            {
                var oldPlayer = state.Players.First(player => player.Id == @event.PlayerId);
                var newPlayer = oldPlayer with { Finished = true };

                return state with
                {
                    Players = state.Players.Remove(oldPlayer).Add(newPlayer),
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

        On<GameEvents.RoundFinished>(static (state, @event) => state with { CurrentRound = null });

        On<GameEvents.CardDiscarded>(
            static (state, @event) =>
            {
                var oldCard = state.Cards.First(card => card.Id == @event.CardId);
                var newCard = oldCard with { Location = new CardLocations.DiscardPile() };

                return state with
                {
                    Cards = state.Cards.Remove(oldCard).Add(newCard),
                };
            }
        );

        On<GameEvents.FoolDeclared>(static (state, @event) => state with { FoolId = @event.PlayerId });
    }

    public ImmutableHashSet<Player> Players { get; init; } = ImmutableHashSet<Player>.Empty;
    public ImmutableHashSet<Card> Cards { get; init; } = ImmutableHashSet<Card>.Empty;
    public ImmutableList<GameOption> Options { get; init; } = ImmutableList<GameOption>.Empty;
    public Lobby? Lobby { get; init; }
    public string? LastDealtCardId { get; init; }
    public CardSuit? CommonTrumpSuit { get; init; }
    public Round? CurrentRound { get; init; }
    public string? FoolId { get; init; }

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
}
