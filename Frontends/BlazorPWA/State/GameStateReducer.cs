using GameService.Api.Data;
using GameService.Api.Data.GameOptions;
using GameService.Api.GameEvents;
using GameService.Api.PossibleMoves;
using Google.Protobuf.WellKnownTypes;

namespace BlazorPWA.State;

public static class GameStateReducer
{
    public static void Apply(GameState state, Any payload)
    {
        if (payload.Is(GameCreated.Descriptor))
        {
            Apply(state, payload.Unpack<GameCreated>());
            return;
        }
        if (payload.Is(PlayerJoined.Descriptor))
        {
            Apply(state, payload.Unpack<PlayerJoined>());
            return;
        }
        if (payload.Is(CommonTrumpCardChosen.Descriptor))
        {
            Apply(state, payload.Unpack<CommonTrumpCardChosen>());
            return;
        }
        if (payload.Is(CardsInTalonShuffled.Descriptor))
        {
            Apply(state, payload.Unpack<CardsInTalonShuffled>());
            return;
        }
        if (payload.Is(CardsDealtToPlayer.Descriptor))
        {
            Apply(state, payload.Unpack<CardsDealtToPlayer>());
            return;
        }
        if (payload.Is(RoundStarted.Descriptor))
        {
            Apply(state, payload.Unpack<RoundStarted>());
            return;
        }
        if (payload.Is(YourTurnStarted.Descriptor))
        {
            Apply(state, payload.Unpack<YourTurnStarted>());
            return;
        }
        if (payload.Is(OpponentTurnStarted.Descriptor))
        {
            Apply(state, payload.Unpack<OpponentTurnStarted>());
            return;
        }
        if (payload.Is(AttackCardPlayed.Descriptor))
        {
            Apply(state, payload.Unpack<AttackCardPlayed>());
            return;
        }
        if (payload.Is(AttackCardBeaten.Descriptor))
        {
            Apply(state, payload.Unpack<AttackCardBeaten>());
            return;
        }
        if (payload.Is(PlayerTookCardsFromTable.Descriptor))
        {
            Apply(state, payload.Unpack<PlayerTookCardsFromTable>());
            return;
        }
        if (payload.Is(CardsOnTableDiscarded.Descriptor))
        {
            Apply(state, payload.Unpack<CardsOnTableDiscarded>());
            return;
        }
        if (payload.Is(DefenceGivenUp.Descriptor))
        {
            Apply(state, payload.Unpack<DefenceGivenUp>());
            return;
        }
        if (payload.Is(RoundFinished.Descriptor))
        {
            Apply(state, payload.Unpack<RoundFinished>());
            return;
        }
        if (payload.Is(FoolDeclared.Descriptor))
        {
            Apply(state, payload.Unpack<FoolDeclared>());
        }
    }

    private static void Apply(GameState state, GameCreated evt)
    {
        state.Table.Clear();
        state.CardsById.Clear();
        state.YourHand.Clear();
        state.YourPossibleMoves.Clear();
        state.TalonCount = evt.CardsInTalon.Count;
        state.TotalPlayerSeats = null;

        foreach (var option in evt.Options)
        {
            if (!option.Is(AmountOfPlayers.Descriptor))
            {
                continue;
            }

            state.TotalPlayerSeats = Math.Max(0, option.Unpack<AmountOfPlayers>().Count);
            break;
        }
    }

    private static void Apply(GameState state, PlayerJoined evt)
    {
        state.Players[evt.PlayerId] = new PlayerState(evt.PlayerId, evt.Seat);
    }

    private static void Apply(GameState state, CommonTrumpCardChosen evt)
    {
        state.TrumpCard = evt.ChosenCard;
        if (!state.CardsById.ContainsKey(evt.ChosenCard.Id))
        {
            state.CardsById[evt.ChosenCard.Id] = new Card(evt.ChosenCard.Id, evt.ChosenCard.Suit, evt.ChosenCard.Rank);
        }
    }

    private static void Apply(GameState state, CardsInTalonShuffled evt)
    {
        state.TalonCount = evt.Cards.Count;
    }

    private static void Apply(GameState state, CardsDealtToPlayer evt)
    {
        state.TalonCount = Math.Max(0, state.TalonCount - evt.DealtCards.Count);

        if (state.Players.TryGetValue(evt.PlayerId, out var player))
        {
            player.CardCount = Math.Max(0, player.CardCount + evt.DealtCards.Count);
        }

        if (evt.PlayerId == state.LocalPlayerId)
        {
            foreach (var anyCard in evt.DealtCards)
            {
                if (anyCard.Is(RegularCard.Descriptor))
                {
                    var cardMsg = anyCard.Unpack<RegularCard>();
                    var card = new Card(cardMsg.Id, cardMsg.Suit, cardMsg.Rank);
                    state.CardsById[card.Id] = card;
                    state.YourHand.Add(card);
                }
            }
        }
    }

    private static void Apply(GameState state, RoundStarted evt)
    {
        state.AttackerPlayerId = evt.AttackerPlayerId;
        state.DefenderPlayerId = evt.DefenderPlayerId;
        state.Table.Clear();
    }

    private static void Apply(GameState state, YourTurnStarted evt)
    {
        state.CurrentTurnPlayerId = evt.PlayerId;
        state.YourPossibleMoves.Clear();
        foreach (var moveAny in evt.PossibleMoves)
        {
            if (moveAny.Is(PlayAttackCard.Descriptor))
            {
                var move = moveAny.Unpack<PlayAttackCard>();
                state.YourPossibleMoves.Add(new PossibleMove(PossibleMoveType.PlayAttackCard, move.YourCardId, null));
            }
            else if (moveAny.Is(BeatAttackCard.Descriptor))
            {
                var move = moveAny.Unpack<BeatAttackCard>();
                state.YourPossibleMoves.Add(
                    new PossibleMove(PossibleMoveType.BeatAttackCard, move.YourCardId, move.TargetCardId)
                );
            }
            else if (moveAny.Is(GiveUpDefence.Descriptor))
            {
                state.YourPossibleMoves.Add(new PossibleMove(PossibleMoveType.GiveUpDefence, null, null));
            }
        }
    }

    private static void Apply(GameState state, OpponentTurnStarted evt)
    {
        state.CurrentTurnPlayerId = evt.PlayerId;
        state.YourPossibleMoves.Clear();
    }

    private static void Apply(GameState state, AttackCardPlayed evt)
    {
        var card = ParseRegularCard(evt.PlayedCard);
        state.CardsById[card.Id] = card;
        state.Table.Add(new TablePair(card, null));

        if (state.Players.TryGetValue(evt.PlayerId, out var player))
        {
            player.CardCount = Math.Max(0, player.CardCount - 1);
        }

        var handIndex = state.YourHand.FindIndex(c => c.Id == card.Id);
        if (handIndex >= 0)
        {
            state.YourHand.RemoveAt(handIndex);
        }
    }

    private static void Apply(GameState state, AttackCardBeaten evt)
    {
        var defender = ParseRegularCard(evt.DefenderCard);
        state.CardsById[defender.Id] = defender;

        var pairIndex = state.Table.FindIndex(p => p.Attack.Id == evt.AttackerCardId);
        if (pairIndex >= 0)
        {
            state.Table[pairIndex] = state.Table[pairIndex] with { Defence = defender };
        }

        if (state.Players.TryGetValue(evt.PlayerId, out var player))
        {
            player.CardCount = Math.Max(0, player.CardCount - 1);
        }

        var handIndex = state.YourHand.FindIndex(c => c.Id == defender.Id);
        if (handIndex >= 0)
        {
            state.YourHand.RemoveAt(handIndex);
        }
    }

    private static void Apply(GameState state, PlayerTookCardsFromTable evt)
    {
        if (evt.PlayerId == state.LocalPlayerId)
        {
            foreach (var cardId in evt.CardIds)
            {
                if (state.CardsById.TryGetValue(cardId, out var card))
                {
                    state.YourHand.Add(card);
                }
            }
        }

        state.Table.Clear();

        if (state.Players.TryGetValue(evt.PlayerId, out var player))
        {
            player.CardCount = Math.Max(0, player.CardCount + evt.CardIds.Count);
        }
    }

    private static void Apply(GameState state, CardsOnTableDiscarded evt)
    {
        state.Table.Clear();
    }

    private static void Apply(GameState state, DefenceGivenUp evt)
    {
        state.CurrentTurnPlayerId = evt.PlayerId;
    }

    private static void Apply(GameState state, RoundFinished evt)
    {
        state.Table.Clear();
        state.YourPossibleMoves.Clear();
    }

    private static void Apply(GameState state, FoolDeclared evt)
    {
        if (state.Players.TryGetValue(evt.PlayerId, out var player))
        {
            player.IsFool = true;
        }
    }

    private static Card ParseRegularCard(Any anyCard)
    {
        if (anyCard.Is(RegularCard.Descriptor))
        {
            var card = anyCard.Unpack<RegularCard>();
            return new Card(card.Id, card.Suit, card.Rank);
        }

        throw new InvalidOperationException("Only RegularCard is supported in MVP.");
    }
}
