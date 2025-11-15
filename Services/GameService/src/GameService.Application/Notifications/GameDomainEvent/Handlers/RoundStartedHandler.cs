namespace GameService.Application;

public class RoundStartedHandler(IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.RoundStarted>>
{
    public Task Handle(
        GameDomainEventNotification<Domain.GameEvents.RoundStarted> notification,
        CancellationToken cancellationToken
    )
    {
        var game = gameCollection.Get(notification.GameId);

        game.Execute(new Domain.GameCommands.ChangeActivePlayer(notification.DomainEvent.AttackerId));

        return Task.CompletedTask;
    }
}
