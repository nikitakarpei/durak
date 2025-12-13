using System.Collections.Immutable;
using BotService.Domain.Interfaces;

namespace BotService.Domain;

public class GameStateProjector : IGameEventsProjector<GameState>
{
    public GameState Apply(GameState state, object @event)
    {
        switch (@event)
        {
            case GameEvents.GameCreated created:
            {
                return state with
                {
                    Options = created.Options.ToImmutableList(),
                    Cards = created
                        .CardsInTalon.Select(
                            Card (cardDto) => new UnrevealedCard(cardDto.Id, new CardLocations.Talon(cardDto.Position))
                        )
                        .ToImmutableHashSet(),
                };
            }

            case GameEvents.CardsInTalonShuffled shuffled:
            {
                return Enumerable.Aggregate(
                    shuffled.Cards,
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

            case GameEvents.CardsDealtToPlayer dealt:
            {
                var newState = state;

                foreach (var cardDto in dealt.DealtCards)
                {
                    var oldCard = newState.Cards.First(card => card.Id == cardDto.Id);
                    var newCard = oldCard with { Location = new CardLocations.Hand(dealt.PlayerId) };

                    switch (cardDto)
                    {
                        case GameEvents.CardsDealtToPlayer.RegularCard regularCardDto:
                            newCard = new RegularCard(
                                regularCardDto.Id,
                                newCard.Location,
                                regularCardDto.Suit,
                                regularCardDto.Rank
                            );
                            break;
                        case GameEvents.CardsDealtToPlayer.JokerCard jokerCardDto:
                            newCard = new JokerCard(jokerCardDto.Id, newCard.Location, jokerCardDto.Color);
                            break;
                        case GameEvents.CardsDealtToPlayer.UnrevealedCard:
                            break;
                    }

                    newState = newState with { Cards = newState.Cards.Remove(oldCard).Add(newCard) };
                }

                return newState;
            }

            case GameEvents.CommonTrumpCardChosen commonTrumpCardChosen:
            {
                return state with { CommonTrumpSuit = commonTrumpCardChosen.ChosenCard.Suit };
            }

            case GameEvents.RoundStarted roundStarted:
            {
                return state with
                {
                    CurrentRound = new Round(
                        roundStarted.AttackerPlayerId,
                        roundStarted.DefenderPlayerId,
                        roundStarted.AttackerPlayerId,
                        roundStarted.DefenderPlayerId,
                        roundStarted.AttackerPlayerId
                    ),
                };
            }

            case GameEvents.YourTurnStarted yourTurnStarted:
            {
                return state with
                {
                    CurrentRound = state.CurrentRound! with { ActivePlayerId = yourTurnStarted.PlayerId },
                };
            }

            case GameEvents.OpponentTurnStarted opponentTurnStarted:
            {
                return state with
                {
                    CurrentRound = state.CurrentRound! with { ActivePlayerId = opponentTurnStarted.PlayerId },
                };
            }

            case GameEvents.AttackCardPlayed cardPlayed:
            {
                var oldCard = state.Cards.First(card => card.Id == cardPlayed.PlayedCard.Id);
                var newCard = oldCard with
                {
                    Location = new CardLocations.Table(cardPlayed.PlayedAt, new CardRolesOnTable.Attack()),
                };

                switch (cardPlayed.PlayedCard)
                {
                    case GameEvents.AttackCardPlayed.RegularCard regularCardDto:
                        newCard = new RegularCard(
                            regularCardDto.Id,
                            newCard.Location,
                            regularCardDto.Suit,
                            regularCardDto.Rank
                        );
                        break;
                    case GameEvents.AttackCardPlayed.JokerCard jokerCardDto:
                        newCard = new JokerCard(jokerCardDto.Id, newCard.Location, jokerCardDto.Color);
                        break;
                }

                return state with
                {
                    Cards = state.Cards.Remove(oldCard).Add(newCard),
                };
            }

            case GameEvents.AttackCardBeaten cardBeaten:
            {
                var oldCard = state.Cards.First(card => card.Id == cardBeaten.DefenderCard.Id);
                var newCard = oldCard with
                {
                    Location = new CardLocations.Table(
                        cardBeaten.PlayedAt,
                        new CardRolesOnTable.Defence(cardBeaten.AttackerCardId)
                    ),
                };

                switch (cardBeaten.DefenderCard)
                {
                    case GameEvents.AttackCardBeaten.RegularCard regularCardDto:
                        newCard = new RegularCard(
                            regularCardDto.Id,
                            newCard.Location,
                            regularCardDto.Suit,
                            regularCardDto.Rank
                        );
                        break;
                    case GameEvents.AttackCardBeaten.JokerCard jokerCardDto:
                        newCard = new JokerCard(jokerCardDto.Id, newCard.Location, jokerCardDto.Color);
                        break;
                }

                return state with
                {
                    Cards = state.Cards.Remove(oldCard).Add(newCard),
                };
            }

            case GameEvents.RoundFinished roundFinished:
            {
                return state with { CurrentRound = null };
            }

            case GameEvents.CardsOnTableDiscarded cardsDiscarded:
            {
                var newState = state;

                foreach (var cardId in cardsDiscarded.CardIds)
                {
                    var oldCard = newState.Cards.First(card => card.Id == cardId);
                    var newCard = oldCard with { Location = new CardLocations.DiscardPile() };

                    newState = newState with { Cards = newState.Cards.Remove(oldCard).Add(newCard) };
                }

                return newState;
            }

            case GameEvents.PlayerTookCardsFromTable playerTookCardsFromTable:
            {
                var newState = state;

                foreach (var cardId in playerTookCardsFromTable.CardIds)
                {
                    var oldCard = newState.Cards.First(card => card.Id == cardId);
                    var newCard = oldCard with { Location = new CardLocations.Hand(playerTookCardsFromTable.PlayerId) };

                    newState = newState with { Cards = newState.Cards.Remove(oldCard).Add(newCard) };
                }

                return newState;
            }

            default:
                return state;
        }
    }
}
