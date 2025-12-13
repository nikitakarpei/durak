using Eventuous;

namespace GameService.Domain;

public class GameAggregate : Aggregate<GameState>
{
    public void Create(ITalonCreator talonCreator, byte[] RandomSeed, GameOption[] Options)
    {
        if (CurrentVersion > 0)
        {
            throw new InvalidOperationException("Game has already been created.");
        }

        var cards = talonCreator.CreateTalon(Options);

        Apply(new GameEvents.GameCreated(RandomSeed, Options, cards));
    }

    public void JoinLobby(string PlayerId)
    {
        if (State.Lobby is not OpenedLobby)
        {
            throw new InvalidOperationException("Lobby is not opened.");
        }

        if (State.Players.Any(player => player.Id == PlayerId))
        {
            throw new InvalidOperationException("Player is already joined.");
        }

        var seat = State.Players.Count;

        var isLastPlayer = State.Players.Count == State.Options.OfType<GameOptions.AmountOfPlayers>().First().Count - 1;

        Apply(new GameEvents.PlayerJoined(PlayerId, seat, isLastPlayer));
    }

    public void ShuffleCardsInTalon(IShuffledPositionsGenerator shuffledPositionsGenerator)
    {
        // todo: надо перемешивать так чтобы в руку игрока не попали карты одной масти

        var originalCards = State
            .Cards.Where(card => card.Location is CardLocations.Talon)
            .OrderBy(card => ((CardLocations.Talon)card.Location).Position)
            .ToArray();

        var (shuffledPositions, newSeed) = shuffledPositionsGenerator.GenerateShuffledPositions(
            State.RandomSeed,
            originalCards.Length
        );

        var shuffledCards = originalCards
            .Select((card, index) => card with { Location = new CardLocations.Talon(shuffledPositions[index]) })
            .ToArray();

        Apply(
            new GameEvents.CardsInTalonShuffled(
                shuffledCards
                    .Select(card => new GameEvents.CardsInTalonShuffled.CardInTalon(
                        card.Id,
                        ((CardLocations.Talon)card.Location).Position
                    ))
                    .ToArray(),
                newSeed
            )
        );
    }

    public void TopUpHand(string PlayerId)
    {
        var minHandSize = State.Options.OfType<GameOptions.MinHandSize>().First().Count;

        var handSize = State.Cards.Count(card => card.Location is CardLocations.Hand hand && hand.PlayerId == PlayerId);

        if (handSize >= minHandSize)
        {
            return;
        }

        var cardsToDraw = minHandSize - handSize;

        var cards = State
            .Cards.Where(card => card.Location is CardLocations.Talon)
            .OrderByDescending(card => ((CardLocations.Talon)card.Location).Position)
            .Take(cardsToDraw);

        if (!cards.Any())
        {
            throw new InvalidOperationException("No cards to draw.");
        }

        Apply(new GameEvents.CardsDealtToPlayer(PlayerId, cards.Select(card => card.Id).ToArray()));
    }

    public void ChooseCommonTrumpSuit()
    {
        // todo: проверить кейс с последней выданной картой
        var card = State.GetBottomCardFromTalon() ?? State.GetLastDealtCard();

        if (card is not RegularCard regularCard)
        {
            return;
        }

        Apply(new GameEvents.CommonTrumpCardChosen(regularCard.Id));
    }

    public void StartFirstRound(IAttackerFinder attackerFinder, IDefenderFinder defenderFinder)
    {
        if (State.PlayedRounds.Count > 0)
        {
            throw new InvalidOperationException("First round has already been played.");
        }

        if (State.CurrentRound != null)
        {
            throw new InvalidOperationException("First round has already been started.");
        }

        var attacker = attackerFinder.FindFirstAttackerForFirstRound(State);

        var defender = defenderFinder.FindFirstDefender(State, attacker);

        Apply(new GameEvents.RoundStarted(1, attacker.Id, defender.Id));
    }

    public void PlayAttackCard(ICardMoveValidator cardMoveValidator, string PlayerId, string CardId)
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.CurrentRound.CurrentAttackerId != PlayerId)
        {
            throw new InvalidOperationException("The player is not the attacker.");
        }

        if (State.CurrentRound.ActivePlayerId != PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        var attackerCard = State.Cards.First(card => card.Id == CardId);

        if (attackerCard.Location is not CardLocations.Hand hand || hand.PlayerId != PlayerId)
        {
            throw new InvalidOperationException("The card is not in a hand.");
        }

        if (!cardMoveValidator.CanCardBePlayed(State, attackerCard))
        {
            throw new InvalidOperationException("Card can not be played.");
        }

        Apply(new GameEvents.AttackCardPlayed(PlayerId, CardId, DateTime.UtcNow));
    }

    public void BeatAttackCard(
        ICardMoveValidator cardMoveValidator,
        string PlayerId,
        string AttackerCardId,
        string DefenderCardId
    )
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.CurrentRound.CurrentDefenderId != PlayerId)
        {
            throw new InvalidOperationException("The player is not the defender.");
        }

        if (State.CurrentRound.ActivePlayerId != PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        var attackerCard = State.Cards.First(card => card.Id == AttackerCardId);

        if (attackerCard.Location is not CardLocations.Table { Role: CardRolesOnTable.Attack })
        {
            throw new InvalidOperationException("The card is not attacking");
        }

        var defenderCard = State.Cards.First(card => card.Id == DefenderCardId);

        if (defenderCard.Location is not CardLocations.Hand hand || hand.PlayerId != PlayerId)
        {
            throw new InvalidOperationException("The card is not in a hand.");
        }

        if (!cardMoveValidator.CanCardBeBeaten(State, attackerCard, defenderCard))
        {
            throw new InvalidOperationException("Card can not be played.");
        }

        Apply(new GameEvents.AttackCardBeaten(PlayerId, attackerCard.Id, defenderCard.Id, DateTime.UtcNow));
    }

    public void GiveUpDefence(string PlayerId)
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.CurrentRound.CurrentDefenderId != PlayerId)
        {
            throw new InvalidOperationException("The player is not the defender.");
        }

        if (State.CurrentRound.ActivePlayerId != PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        Apply(new GameEvents.DefenceGivenUp(PlayerId));
    }

    public void StartNextRound(IAttackerFinder attackerFinder, IDefenderFinder defenderFinder)
    {
        if (State.CurrentRound != null)
        {
            throw new InvalidOperationException("Current round is not finished.");
        }

        if (State.PlayedRounds.Count == 0)
        {
            throw new InvalidOperationException("No rounds have been played yet.");
        }

        var previousRound = State.PlayedRounds.Last();

        var newAttacker = attackerFinder.FindFirstAttackerForNextRound(State, previousRound);

        var newDefender = defenderFinder.FindFirstDefender(State, newAttacker);

        Apply(new GameEvents.RoundStarted(previousRound.RoundNumber + 1, newAttacker.Id, newDefender.Id));
    }

    public void DeclareFool(string PlayerId)
    {
        if (State.FoolId is not null)
        {
            throw new InvalidOperationException("Fool has already been declared.");
        }

        Apply(new GameEvents.FoolDeclared(PlayerId));
    }

    public void ChangeActivePlayer(IPossibleMovesFinder possibleMovesFinder, string PlayerId)
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.CurrentRound.ActivePlayerId == PlayerId)
        {
            throw new InvalidOperationException("The player is already the active player.");
        }

        var possibleMoves = possibleMovesFinder.FindPossibleMoves(State, PlayerId);

        if (possibleMoves.Count == 0)
        {
            throw new InvalidOperationException("The player has no possible moves.");
        }

        var playerAfkTimerDuration = State.Options.OfType<GameOptions.PlayerAfkTimerDuration>().First().Duration;

        var currentPlayerAfkTimerGeneration = (State.PlayerAfkTimer as ActivePlayerAfkTimer)?.TimerGeneration;
        var nextPlayerAfkTimerGeneration = currentPlayerAfkTimerGeneration.HasValue
            ? currentPlayerAfkTimerGeneration.Value + 1
            : 1;

        Apply(
            new GameEvents.ActivePlayerChanged(
                PlayerId,
                possibleMoves.ToArray(),
                playerAfkTimerDuration,
                currentPlayerAfkTimerGeneration,
                nextPlayerAfkTimerGeneration
            )
        );
    }

    public void FinishCurrentRound()
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.PlayerAfkTimer is not ActivePlayerAfkTimer activePlayerAfkTimer)
        {
            throw new InvalidOperationException("Player AFK timer is not active. This should not happen.");
        }

        var playerAfkTimerGeneration = activePlayerAfkTimer.TimerGeneration;

        Apply(new GameEvents.RoundFinished(State.CurrentRound.RoundNumber, playerAfkTimerGeneration));
    }

    public void DiscardCardsOnTable()
    {
        var cardIds = State.CardsOnTable().Select(card => card.Id).ToArray();

        if (cardIds.Length == 0)
        {
            throw new InvalidOperationException("There are no cards on the table.");
        }

        Apply(new GameEvents.CardsOnTableDiscarded(cardIds));
    }

    public void TakeCardsFromTable(string PlayerId)
    {
        var cardIds = State.CardsOnTable().Select(card => card.Id).ToArray();

        if (cardIds.Length == 0)
        {
            throw new InvalidOperationException("There are no cards on the table.");
        }

        Apply(new GameEvents.PlayerTookCardsFromTable(PlayerId, cardIds));
    }
}
