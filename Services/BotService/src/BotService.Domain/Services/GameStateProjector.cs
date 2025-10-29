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

            case GameEvents.CardDealtToPlayer dealt:
                {
                    var oldCard = state.Cards.First(card => card.Id == dealt.DealtCard.Id);
                    var newCard = oldCard with { Location = new CardLocations.Hand(dealt.PlayerId) };

                    switch (dealt.DealtCard)
                    {
                        case GameEvents.CardDealtToPlayer.RegularCard regularCardDto:
                            newCard = new RegularCard(
                                regularCardDto.Id,
                                newCard.Location,
                                regularCardDto.Suit,
                                regularCardDto.Rank
                            );
                            break;
                        case GameEvents.CardDealtToPlayer.JokerCard jokerCardDto:
                            newCard = new JokerCard(jokerCardDto.Id, newCard.Location, jokerCardDto.Color);
                            break;
                        case GameEvents.CardDealtToPlayer.UnrevealedCard:
                            break;
                    }

                    return state with
                    {
                        Cards = state.Cards.Remove(oldCard).Add(newCard),
                    };
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

            case GameEvents.CardDiscarded cardDiscarded:
                {
                    var oldCard = state.Cards.First(card => card.Id == cardDiscarded.CardId);
                    var newCard = oldCard with { Location = new CardLocations.DiscardPile() };

                    return state with
                    {
                        Cards = state.Cards.Remove(oldCard).Add(newCard),
                    };
                }

            default:
                return state;
        }
    }
}
