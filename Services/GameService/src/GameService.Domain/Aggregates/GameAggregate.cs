using System.Collections.Immutable;
using Eventuous;

namespace GameService.Domain;

public class GameAggregate(IRandomNumberGenerator randomNumberGenerator, IPossibleMovesFinder possibleMovesFinder)
    : Aggregate<GameState>
{
    public void When(GameCommands.CreateGame createGame)
    {
        var cards = CreateTalon(createGame.Options);

        Apply(new GameEvents.GameCreated(createGame.RandomSeed, createGame.Options, cards));
    }

    public void When(GameCommands.OpenLobby openLobby)
    {
        if (State.Lobby != null)
        {
            throw new InvalidOperationException("Lobby is already opened.");
        }

        // todo: лобби открывается один раз

        Apply(new GameEvents.LobbyOpened());
    }

    public void When(GameCommands.JoinLobby joinLobby)
    {
        if (State.Lobby == null)
        {
            throw new InvalidOperationException("Lobby is not opened.");
        }

        if (State.Players.Any(player => player.Id == joinLobby.PlayerId))
        {
            throw new InvalidOperationException("Player is already joined.");
        }

        var seat = State.Players.Count;

        Apply(new GameEvents.PlayerJoined(joinLobby.PlayerId, seat));

        var amountOfPlayersOption = State.Options.OfType<GameOptions.AmountOfPlayers>().First();

        if (amountOfPlayersOption.Count == State.Players.Count)
        {
            Apply(new GameEvents.AllPlayersJoined());
        }
    }

    public void When(GameCommands.StartFirstRound startFirstRound)
    {
        if (State.PlayedRounds.Count > 0)
        {
            throw new InvalidOperationException("First round has already been played.");
        }

        if (State.CurrentRound != null)
        {
            throw new InvalidOperationException("First round has already been started.");
        }

        ShuffleCardsInTalon();

        DealInitialCards();

        ChooseCommonTrumpSuit();

        var attacker = FindFirstAttackerForFirstRound();

        var defender = FindFirstDefender(attacker);

        Apply(new GameEvents.RoundStarted(attacker.Id, defender.Id));

        ChangeActivePlayer(attacker.Id);
    }

    public void When(GameCommands.PlayAttackCard playAttackCard)
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.CurrentRound.CurrentAttackerId != playAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The player is not the attacker.");
        }

        if (State.CurrentRound.ActivePlayerId != playAttackCard.PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        var attackerCard = State.Cards.First(card => card.Id == playAttackCard.CardId);

        if (attackerCard.Location is not CardLocations.Hand hand || hand.PlayerId != playAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The card is not in a hand.");
        }

        if (!CanCardBePlayed(attackerCard))
        {
            throw new InvalidOperationException("Card can not be played.");
        }

        Apply(new GameEvents.AttackCardPlayed(playAttackCard.PlayerId, attackerCard.Id, DateTime.UtcNow));

        var isPlayerHandEmpty = State.IsPlayerHandEmpty(playAttackCard.PlayerId);

        if (isPlayerHandEmpty && State.IsCardTalonEmpty())
        {
            Apply(new GameEvents.PlayerFinished(playAttackCard.PlayerId));

            if (TryFindFool())
                return;
        }

        if (isPlayerHandEmpty)
        {
            // todo: если подкидной, то переводим атаку на другого игрока

            ChangeActivePlayer(State.CurrentRound!.CurrentDefenderId);
        }
        else
        {
            // todo: если подкидной, то игрок продолжает ходить

            ChangeActivePlayer(State.CurrentRound!.CurrentDefenderId);
        }
    }

    public void When(GameCommands.BeatAttackCard beatAttackCard)
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.CurrentRound.CurrentDefenderId != beatAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The player is not the defender.");
        }

        if (State.CurrentRound.ActivePlayerId != beatAttackCard.PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        var attackerCard = State.Cards.First(card => card.Id == beatAttackCard.AttackerCardId);

        if (attackerCard.Location is not CardLocations.Table { Role: CardRolesOnTable.Attack })
        {
            throw new InvalidOperationException("The card is not attacking");
        }

        var defenderCard = State.Cards.First(card => card.Id == beatAttackCard.DefenderCardId);

        if (defenderCard.Location is not CardLocations.Hand hand || hand.PlayerId != beatAttackCard.PlayerId)
        {
            throw new InvalidOperationException("The card is not in a hand.");
        }

        if (!CanCardBeBeaten(attackerCard, defenderCard))
        {
            throw new InvalidOperationException("Card can not be played.");
        }

        Apply(
            new GameEvents.AttackCardBeaten(beatAttackCard.PlayerId, attackerCard.Id, defenderCard.Id, DateTime.UtcNow)
        );

        if (State.IsPlayerHandEmpty(beatAttackCard.PlayerId) && State.IsCardTalonEmpty())
        {
            Apply(new GameEvents.PlayerFinished(beatAttackCard.PlayerId));

            if (TryFindFool())
                return;
        }

        if (State.AreAllCardsBeaten())
        {
            // todo: если подкидной, то ставим атакующим первого игрока или следующего после него

            FinishCurrentRound();
        }
    }

    public void When(GameCommands.ConcludeAttack concludeAttack)
    {
        throw new NotImplementedException();
    }

    public void When(GameCommands.GiveUpDefence giveUpDefence)
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no round.");
        }

        if (State.CurrentRound.CurrentDefenderId != giveUpDefence.PlayerId)
        {
            throw new InvalidOperationException("The player is not the defender.");
        }

        if (State.CurrentRound.ActivePlayerId != giveUpDefence.PlayerId)
        {
            throw new InvalidOperationException("It's the other player's turn now");
        }

        Apply(new GameEvents.DefenceGivenUp(giveUpDefence.PlayerId));

        // todo: если подкидной, то переводим ход на атакующего

        FinishCurrentRound();
    }

    public void When(GameCommands.StartNextRound startNextRound)
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

        var newAttacker = FindFirstAttackerForNextRound(previousRound);

        var newDefender = FindFirstDefender(newAttacker);

        Apply(new GameEvents.RoundStarted(newAttacker.Id, newDefender.Id));

        ChangeActivePlayer(newAttacker.Id);
    }

    // -----------------------------------------------------

    private Card[] CreateTalon(GameOption[] options)
    {
        var ranksOption =
            options.OfType<GameOptions.CardRanksInTalon>().FirstOrDefault() ?? throw new InvalidOperationException(
                "Card ranks in talon option is not set."
            );
        var ranks = ranksOption.Ranks;

        var nextPosition = 0;

        return Enum.GetValues<CardSuit>()
            .Order()
            .SelectMany(s =>
                ranks
                    .Order()
                    .Select(r => new RegularCard(
                        Guid.NewGuid().ToString(),
                        new CardLocations.Talon(nextPosition++),
                        s,
                        r
                    ))
                    .Cast<Card>()
            )
            .ToArray();
    }

    private void ShuffleCardsInTalon()
    {
        // todo: надо перемешивать так чтобы в руку игрока не попали карты одной масти

        var originalCards = State
            .Cards.Where(card => card.Location is CardLocations.Talon)
            .OrderBy(card => ((CardLocations.Talon)card.Location).Position)
            .ToArray();

        var (shuffledPositions, newSeed) = GenerateShuffledPositions(originalCards.Length);

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

    private (int[] Positions, byte[] NewSeed) GenerateShuffledPositions(int count)
    {
        var seed = State.RandomSeed;

        var positions = Enumerable.Range(0, count).ToArray();
        for (var i = positions.Length - 1; i >= 1; i--)
        {
            var (j, newSeed) = randomNumberGenerator.GetInt32(seed, i + 1);
            seed = newSeed;
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        return (positions, seed);
    }

    private void DealInitialCards()
    {
        foreach (var player in State.Players.OrderBy(player => player.Seat))
        {
            TopUpHand(player.Id);
        }
    }

    private void TopUpHand(string playerId)
    {
        var minHandSize = State.Options.OfType<GameOptions.MinHandSize>().First().Count;

        var handSize = State.Cards.Count(card => card.Location is CardLocations.Hand hand && hand.PlayerId == playerId);

        if (handSize >= minHandSize)
        {
            return;
        }

        var cardsToDraw = minHandSize - handSize;

        var cards = State
            .Cards.Where(card => card.Location is CardLocations.Talon)
            .OrderByDescending(card => ((CardLocations.Talon)card.Location).Position)
            .Take(cardsToDraw);

        foreach (var card in cards)
        {
            Apply(new GameEvents.CardDealtToPlayer(playerId, card.Id));
        }
    }

    private void ChooseCommonTrumpSuit()
    {
        // todo: проверить кейс с последней выданной картой
        var card = State.GetBottomCardFromTalon() ?? State.GetLastDealtCard();

        if (card is not RegularCard regularCard)
        {
            return;
        }

        Apply(new GameEvents.CommonTrumpCardChosen(regularCard.Id));
    }

    private Player FindFirstAttackerForFirstRound()
    {
        // todo: первый атакующий должен выбираться по младшему козырю

        return State.Players.First(player => player.Seat == 0);
    }

    private Player FindFirstDefender(Player attacker)
    {
        Player? defender = null;

        for (var iteration = 0; iteration < State.Players.Count; iteration++)
        {
            defender = State.GetNextPlayerClockwise(defender?.Seat ?? attacker.Seat);

            if (defender.Finished)
                continue;

            return defender;
        }

        throw new InvalidOperationException("Cannot find attacker for next round");
    }

    private bool CanCardBePlayed(Card attackerCard)
    {
        if (State.HasCardsOnTable())
        {
            // todo: в подкидном можно подкидывать карты того же достоинства что и на столе

            throw new NotImplementedException();
        }

        return true;
    }

    private bool TryFindFool()
    {
        var unfinishedPlayers = State.Players.Where(player => !player.Finished).ToImmutableList();

        if (unfinishedPlayers.Count != 1)
        {
            return false;
        }

        var fool = unfinishedPlayers.First();

        Apply(new GameEvents.FoolDeclared(fool.Id));

        return true;
    }

    private bool CanCardBeBeaten(Card attackerCard, Card defenderCard)
    {
        if (attackerCard is RegularCard attackerRegularCard && defenderCard is RegularCard defenderRegularCard)
        {
            if (defenderRegularCard.Suit == attackerRegularCard.Suit)
            {
                return defenderRegularCard.Rank > attackerRegularCard.Rank;
            }

            if (State.CommonTrumpSuit != null)
            {
                return defenderRegularCard.Suit == State.CommonTrumpSuit;
            }

            return false;
        }

        throw new NotImplementedException();
    }

    private void DiscardCardsOnTable()
    {
        foreach (var card in State.CardsOnTableByPlayOrder())
        {
            Apply(new GameEvents.CardDiscarded(card.Id));
        }
    }

    private void FinishCurrentRound()
    {
        if (State.CurrentRound == null)
        {
            throw new InvalidOperationException("There is no current round.");
        }

        if (State.CurrentRound.DefenceGivenUp)
        {
            GiveTableCardsToPlayer(State.CurrentRound.CurrentDefenderId);
        }
        else
        {
            DiscardCardsOnTable();
        }

        TopUpHand(State.CurrentRound.FirstAttackerId);

        // todo: раздать карты всем атакующим (раздавать всем по часовой от первого атакующего или хранить в раунде историю кто в какой очереди атаковал?)

        TopUpHand(State.CurrentRound.CurrentDefenderId);

        Apply(new GameEvents.RoundFinished());
    }

    private void GiveTableCardsToPlayer(string playerId)
    {
        foreach (var card in State.CardsOnTableByPlayOrder())
        {
            Apply(new GameEvents.CardDealtToPlayer(playerId, card.Id));
        }
    }

    private Player FindFirstAttackerForNextRound(Round previousRound)
    {
        Player? nextAttacker = null;

        for (var iteration = 0; iteration < State.Players.Count; iteration++)
        {
            if (nextAttacker == null)
            {
                nextAttacker = State.Players.First(player => player.Id == previousRound.CurrentDefenderId);

                if (previousRound.DefenceGivenUp)
                    continue;
            }
            else
            {
                nextAttacker = State.GetNextPlayerClockwise(nextAttacker.Seat);
            }

            if (nextAttacker.Finished)
                continue;

            return nextAttacker;
        }

        throw new InvalidOperationException("Cannot find attacker for next round");
    }

    private void ChangeActivePlayer(string playerId)
    {
        var possibleMoves = possibleMovesFinder.FindPossibleMoves(State, playerId);
        Apply(new GameEvents.ActivePlayerChanged(playerId, possibleMoves.ToArray()));
    }
}
