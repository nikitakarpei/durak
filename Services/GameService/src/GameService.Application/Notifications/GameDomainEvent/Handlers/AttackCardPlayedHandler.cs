namespace GameService.Application;

public class AttackCardPlayedHandler(Domain.IFoolFinder foolFinder, IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.AttackCardPlayed>>
{
    public Task Handle(
        GameDomainEventNotification<Domain.GameEvents.AttackCardPlayed> notification,
        CancellationToken cancellationToken
    )
    {
        var state = notification.NewState;
        var game = gameCollection.Get(notification.GameId);

        var fool = foolFinder.TryFindFool(state);

        if (fool is not null)
        {
            game.Execute(new Domain.GameCommands.DeclareFool(fool.Id));
        }
        else
        {
            if (state.IsPlayerHandEmpty(notification.DomainEvent.PlayerId))
            {
                // todo: если подкидной, то переводим атаку на другого игрока

                game.Execute(new Domain.GameCommands.ChangeActivePlayer(state.CurrentRound!.CurrentDefenderId));
            }
            else
            {
                // todo: если подкидной, то игрок продолжает ходить

                game.Execute(new Domain.GameCommands.ChangeActivePlayer(state.CurrentRound!.CurrentDefenderId!));
            }
        }

        return Task.CompletedTask;
    }
}
