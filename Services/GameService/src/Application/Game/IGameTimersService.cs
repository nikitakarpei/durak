namespace GameService.Application;

public interface IGameTimersService
{
    public Task StartTimerAsync(
        string gameId,
        int triggeredByEventNumber,
        string timerType,
        int timerGeneration,
        DateTime expiresAt,
        CancellationToken cancellationToken
    );

    public Task CancelTimerAsync(
        string gameId,
        int triggeredByEventNumber,
        string timerType,
        int timerGeneration,
        CancellationToken cancellationToken
    );
}
