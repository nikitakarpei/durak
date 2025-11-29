namespace GameService.Application;

public class RestartPlayerAfkTimerOnActivePlayerChangedHandler(IGameTimersService gameTimersService)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.ActivePlayerChanged>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.ActivePlayerChanged> notification,
        CancellationToken cancellationToken
    )
    {
        if (notification.DomainEvent.CurrentPlayerAfkTimerGeneration.HasValue)
        {
            await gameTimersService.CancelTimerAsync(
                notification.GameId,
                notification.EventNumber,
                "player-afk-timer",
                notification.DomainEvent.CurrentPlayerAfkTimerGeneration.Value,
                cancellationToken
            );
        }

        // todo: use time from RecordedEvent instead of current time
        var expiresAt = DateTime.UtcNow.Add(notification.DomainEvent.PlayerAfkTimerDuration);

        await gameTimersService.StartTimerAsync(
            notification.GameId,
            notification.EventNumber,
            "player-afk-timer",
            notification.DomainEvent.NextPlayerAfkTimerGeneration,
            expiresAt,
            cancellationToken
        );
    }
}
