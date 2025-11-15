namespace GameService.Application;

public class FoolDeclaredHandler(IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.FoolDeclared>>
{
    public Task Handle(
        GameDomainEventNotification<Domain.GameEvents.FoolDeclared> notification,
        CancellationToken cancellationToken
    )
    {
        gameCollection.Delete(notification.GameId);
        return Task.CompletedTask;
    }
}
