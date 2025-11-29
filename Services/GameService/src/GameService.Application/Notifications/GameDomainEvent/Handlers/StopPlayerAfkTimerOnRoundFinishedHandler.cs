namespace GameService.Application;

public class StopPlayerAfkTimerOnRoundFinishedHandler(IGameTimersService gameTimersService)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.RoundFinished>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.RoundFinished> notification,
        CancellationToken cancellationToken
    )
    {
        await gameTimersService.CancelTimerAsync(
            notification.GameId,
            notification.EventNumber,
            "player-afk-timer",
            notification.DomainEvent.PlayerAfkTimerGeneration,
            cancellationToken
        );
    }
}
