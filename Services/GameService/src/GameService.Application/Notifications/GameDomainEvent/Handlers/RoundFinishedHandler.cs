namespace GameService.Application;

public class RoundFinishedHandler(IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.RoundFinished>>
{
    public Task Handle(
        GameDomainEventNotification<Domain.GameEvents.RoundFinished> notification,
        CancellationToken cancellationToken
    )
    {
        var game = gameCollection.Get(notification.GameId);

        game.Execute(new Domain.GameCommands.StartNextRound());

        return Task.CompletedTask;
    }
}
