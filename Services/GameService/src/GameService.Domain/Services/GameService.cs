namespace GameService.Domain;

public class GameService(
    ITalonCreator talonCreator,
    IShuffledPositionsGenerator shuffledPositionsGenerator,
    IAttackerFinder attackerFinder,
    IDefenderFinder defenderFinder,
    ICardMoveValidator cardMoveValidator,
    IFoolFinder foolFinder,
    IPossibleMovesFinder possibleMovesFinder
)
{
    #region Command handlers

    public void HandleCommand(GameAggregate game, GameCommands.CreateGame command)
    {
        game.Create(talonCreator, command.RandomSeed, command.Options);
    }

    public void HandleCommand(GameAggregate game, GameCommands.BeatAttackCard command)
    {
        game.BeatAttackCard(cardMoveValidator, command.PlayerId, command.AttackerCardId, command.DefenderCardId);
    }

    public void HandleCommand(GameAggregate game, GameCommands.ConcludeAttack command)
    {
        throw new NotImplementedException();
    }

    public void HandleCommand(GameAggregate game, GameCommands.GiveUpDefence command)
    {
        game.GiveUpDefence(command.PlayerId);
    }

    public void HandleCommand(GameAggregate game, GameCommands.JoinLobby command)
    {
        game.JoinLobby(command.PlayerId);
    }

    public void HandleCommand(GameAggregate game, GameCommands.PlayAttackCard command)
    {
        game.PlayAttackCard(cardMoveValidator, command.PlayerId, command.CardId);
    }

    public void HandleCommand(GameAggregate game, object command)
    {
        throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}");
    }

    #endregion

    #region Event handlers

    public void HandleEvent(GameAggregate game, GameEvents.PlayerJoined @event)
    {
        if (@event.IsLastPlayer)
        {
            game.ShuffleCardsInTalon(shuffledPositionsGenerator);

            foreach (var player in game.State.Players)
            {
                game.TopUpHand(player.Id);
            }

            game.ChooseCommonTrumpSuit();

            game.StartFirstRound(attackerFinder, defenderFinder);
        }
    }

    public void HandleEvent(GameAggregate game, GameEvents.AttackCardPlayed @event)
    {
        var fool = foolFinder.TryFindFool(game.State);

        if (fool is not null)
        {
            game.DeclareFool(fool.Id);

            return;
        }

        if (game.State.IsPlayerHandEmpty(@event.PlayerId))
        {
            // todo: если подкидной, то переводим атаку на другого игрока

            game.ChangeActivePlayer(possibleMovesFinder, game.State.CurrentRound!.CurrentDefenderId);
        }
        else
        {
            // todo: если подкидной, то игрок продолжает ходить

            game.ChangeActivePlayer(possibleMovesFinder, game.State.CurrentRound!.CurrentDefenderId!);
        }
    }

    public void HandleEvent(GameAggregate game, GameEvents.AttackCardBeaten @event)
    {
        var fool = foolFinder.TryFindFool(game.State);

        if (fool is not null)
        {
            game.DeclareFool(fool.Id);

            return;
        }

        if (game.State.AreAllCardsBeaten())
        {
            // todo: если подкидной, то ставим атакующим первого игрока или следующего после него

            game.FinishCurrentRound();
        }
    }

    public void HandleEvent(GameAggregate game, GameEvents.DefenceGivenUp @event)
    {
        // todo: если подкидной, то переводим ход на атакующего

        game.FinishCurrentRound();
    }

    public void HandleEvent(GameAggregate game, GameEvents.RoundStarted @event)
    {
        game.ChangeActivePlayer(possibleMovesFinder, @event.AttackerId);
    }

    public void HandleEvent(GameAggregate game, GameEvents.RoundFinished @event)
    {
        var round = game.State.PlayedRounds.First(round => round.RoundNumber == @event.RoundNumber);

        if (round.DefenceGivenUp)
        {
            game.TakeCardsFromTable(round.CurrentDefenderId);
        }
        else
        {
            game.DiscardCardsOnTable();
        }

        game.TopUpHand(round.FirstAttackerId);

        // todo: раздать карты всем атакующим (раздавать всем по часовой от первого атакующего или хранить в раунде историю кто в какой очереди атаковал?)

        game.TopUpHand(round.CurrentDefenderId);

        game.StartNextRound(attackerFinder, defenderFinder);
    }

    public void HandleEvent(GameAggregate game, object @event) { }

    #endregion
}
