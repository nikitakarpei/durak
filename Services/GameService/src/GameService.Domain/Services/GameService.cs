namespace GameService.Domain;

public sealed class GameService(
    ITalonCreator talonCreator,
    IShuffledPositionsGenerator shuffledPositionsGenerator,
    IPossibleMovesFinder possibleMovesFinder,
    IAttackerFinder attackerFinder,
    IDefenderFinder defenderFinder,
    ICardMoveValidator cardMoveValidator
)
{
    public GameEvents.GameCreated Decide(GameState state, GameCommands.CreateGame createGame)
    {
        var cards = talonCreator.CreateTalon(createGame.Options);

        return new GameEvents.GameCreated(createGame.RandomSeed, createGame.Options, cards);
    }

    public GameEvents.LobbyOpened Decide(GameState state, GameCommands.OpenLobby openLobby)
    {
        if (state.Lobby != null)
        {
            throw new InvalidOperationException("Lobby was already opened before.");
        }

        return new GameEvents.LobbyOpened();
    }

    public GameEvents.LobbyClosed Decide(GameState state, GameCommands.CloseLobby closeLobby)
    {
        if (state.Lobby == null)
        {
            throw new InvalidOperationException("Lobby does not exist.");
        }

        if (state.Lobby is not OpenedLobby)
        {
            throw new InvalidOperationException("Lobby is not opened.");
        }

        return new GameEvents.LobbyClosed();
    }

    public GameEvents.PlayerJoined Decide(GameState state, GameCommands.JoinLobby joinLobby)
    {
        if (state.Lobby == null)
        {
            throw new InvalidOperationException("Lobby is not opened.");
        }

        if (state.Players.Any(player => player.Id == joinLobby.PlayerId))
        {
            throw new InvalidOperationException("Player is already joined.");
        }

        var seat = state.Players.Count;

        return new GameEvents.PlayerJoined(joinLobby.PlayerId, seat);
    }

    public GameEvents.CardsInTalonShuffled Decide(GameState state, GameCommands.ShuffleCardsInTalon shuffleCardsInTalon)
    {
        // todo: надо перемешивать так чтобы в руку игрока не попали карты одной масти

        var originalCards = state
            .Cards.Where(card => card.Location is CardLocations.Talon)
            .OrderBy(card => ((CardLocations.Talon)card.Location).Position)
            .ToArray();

        var (shuffledPositions, newSeed) = shuffledPositionsGenerator.GenerateShuffledPositions(
            state.RandomSeed,
            originalCards.Length
        );

        var shuffledCards = originalCards
            .Select((card, index) => card with { Location = new CardLocations.Talon(shuffledPositions[index]) })
            .ToArray();

        return new GameEvents.CardsInTalonShuffled(
            shuffledCards
                .Select(card => new GameEvents.CardsInTalonShuffled.CardInTalon(
                    card.Id,
                    ((CardLocations.Talon)card.Location).Position
                ))
                .ToArray(),
            newSeed
        );
    }

    public GameEvents.CardsDealtToPlayer? Decide(GameState state, GameCommands.TopUpHand topUpHand)
    {
        var minHandSize = state.Options.OfType<GameOptions.MinHandSize>().First().Count;

        var handSize = state.Cards.Count(card =>
            card.Location is CardLocations.Hand hand && hand.PlayerId == topUpHand.PlayerId
        );

        if (handSize >= minHandSize)
        {
            return null;
        }

        var cardsToDraw = minHandSize - handSize;

        var cards = state
            .Cards.Where(card => card.Location is CardLocations.Talon)
            .OrderByDescending(card => ((CardLocations.Talon)card.Location).Position)
            .Take(cardsToDraw);

        return new GameEvents.CardsDealtToPlayer(topUpHand.PlayerId, cards.Select(card => card.Id).ToArray());
    }

    public GameEvents.CommonTrumpCardChosen? Decide(
        GameState state,
        GameCommands.ChooseCommonTrumpSuit chooseCommonTrumpSuit
    )
    {
        // todo: проверить кейс с последней выданной картой
        var card = state.GetBottomCardFromTalon() ?? state.GetLastDealtCard();

        if (card is not RegularCard regularCard)
        {
            return null;
        }

        return new GameEvents.CommonTrumpCardChosen(regularCard.Id);
    }

    public GameEvent? Decide(GameState state, GameCommands.StartFirstRound startFirstRound)
    {
        if (state.PlayedRounds.Count > 0)
        {
            throw new InvalidOperationException("First round has already been played.");
        }

        if (state.CurrentRound != null)
        {
            throw new InvalidOperationException("First round has already been started.");
        }

        var attacker = attackerFinder.FindFirstAttackerForFirstRound(state);

        var defender = defenderFinder.FindFirstDefender(state, attacker);

        return new GameEvents.RoundStarted(attacker.Id, defender.Id);
    }

    public GameEvents.AttackCardPlayed Decide(GameState state, GameCommands.PlayAttackCard playAttackCard)
    {
        if (state.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (state.CurrentRound.CurrentAttackerId != playAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The player is not the attacker.");
        }

        if (state.CurrentRound.ActivePlayerId != playAttackCard.PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        var attackerCard = state.Cards.First(card => card.Id == playAttackCard.CardId);

        if (attackerCard.Location is not CardLocations.Hand hand || hand.PlayerId != playAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The card is not in a hand.");
        }

        if (!cardMoveValidator.CanCardBePlayed(state, attackerCard))
        {
            throw new InvalidOperationException("Card can not be played.");
        }

        return new GameEvents.AttackCardPlayed(playAttackCard.PlayerId, attackerCard.Id, DateTime.UtcNow);
    }

    public GameEvents.AttackCardBeaten Decide(GameState state, GameCommands.BeatAttackCard beatAttackCard)
    {
        if (state.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (state.CurrentRound.CurrentDefenderId != beatAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The player is not the defender.");
        }

        if (state.CurrentRound.ActivePlayerId != beatAttackCard.PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        var attackerCard = state.Cards.First(card => card.Id == beatAttackCard.AttackerCardId);

        if (attackerCard.Location is not CardLocations.Table { Role: CardRolesOnTable.Attack })
        {
            throw new InvalidOperationException("The card is not attacking");
        }

        var defenderCard = state.Cards.First(card => card.Id == beatAttackCard.DefenderCardId);

        if (defenderCard.Location is not CardLocations.Hand hand || hand.PlayerId != beatAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The card is not in a hand.");
        }

        if (!cardMoveValidator.CanCardBeBeaten(state, attackerCard, defenderCard))
        {
            throw new InvalidOperationException("Card can not be played.");
        }

        return new GameEvents.AttackCardBeaten(
            beatAttackCard.PlayerId,
            attackerCard.Id,
            defenderCard.Id,
            DateTime.UtcNow
        );
    }

    public GameEvent? Decide(GameState state, GameCommands.ConcludeAttack concludeAttack)
    {
        throw new NotImplementedException();
    }

    public GameEvents.DefenceGivenUp Decide(GameState state, GameCommands.GiveUpDefence giveUpDefence)
    {
        if (state.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (state.CurrentRound.CurrentDefenderId != giveUpDefence.PlayerId)
        {
            throw new InvalidOperationException("The player is not the defender.");
        }

        if (state.CurrentRound.ActivePlayerId != giveUpDefence.PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        return new GameEvents.DefenceGivenUp(giveUpDefence.PlayerId);
    }

    public GameEvents.RoundStarted Decide(GameState state, GameCommands.StartNextRound startNextRound)
    {
        if (state.CurrentRound != null)
        {
            throw new InvalidOperationException("Current round is not finished.");
        }

        if (state.PlayedRounds.Count == 0)
        {
            throw new InvalidOperationException("No rounds have been played yet.");
        }

        var previousRound = state.PlayedRounds.Last();

        var newAttacker = attackerFinder.FindFirstAttackerForNextRound(state, previousRound);

        var newDefender = defenderFinder.FindFirstDefender(state, newAttacker);

        return new GameEvents.RoundStarted(newAttacker.Id, newDefender.Id);
    }

    public GameEvents.FoolDeclared? Decide(GameState state, GameCommands.DeclareFool declareFool)
    {
        if (state.FoolId is not null)
        {
            throw new InvalidOperationException("Fool has already been declared.");
        }

        return new GameEvents.FoolDeclared(declareFool.PlayerId);
    }

    public GameEvents.ActivePlayerChanged Decide(GameState state, GameCommands.ChangeActivePlayer changeActivePlayer)
    {
        if (state.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (state.CurrentRound.ActivePlayerId == changeActivePlayer.PlayerId)
        {
            throw new InvalidOperationException("The player is already the active player.");
        }

        var possibleMoves = possibleMovesFinder.FindPossibleMoves(state, changeActivePlayer.PlayerId);

        return new GameEvents.ActivePlayerChanged(changeActivePlayer.PlayerId, possibleMoves.ToArray());
    }

    public GameEvents.RoundFinished Decide(GameState state, GameCommands.FinishCurrentRound finishCurrentRound)
    {
        if (state.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        return new GameEvents.RoundFinished();
    }
}
