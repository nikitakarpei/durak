namespace GameService.Application;

public class NotifyGameOnPlayerAfkTimerExpiredHandler(IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameTimerExpiredNotification>
{
    public async Task Handle(GameTimerExpiredNotification notification, CancellationToken cancellationToken)
    {
        if (notification.TimerType != "player-afk-timer")
        {
            return;
        }

        var game = gameCollection.Get(notification.GameId);
        await game.ExecuteAsync(
            new Domain.GameCommands.HandlePlayerAfkTimerExpired(notification.TimerGeneration),
            cancellationToken
        );
    }
}
