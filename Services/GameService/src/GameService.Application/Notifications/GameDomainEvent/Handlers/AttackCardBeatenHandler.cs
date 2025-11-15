namespace GameService.Application;

public class AttackCardBeatenHandler(Domain.IFoolFinder foolFinder, IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.AttackCardBeaten>>
{
    public Task Handle(
        GameDomainEventNotification<Domain.GameEvents.AttackCardBeaten> notification,
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
            if (state.AreAllCardsBeaten())
            {
                // todo: если подкидной, то ставим атакующим первого игрока или следующего после него

                game.Execute(new Domain.GameCommands.FinishCurrentRound());
            }
        }

        return Task.CompletedTask;
    }
}
