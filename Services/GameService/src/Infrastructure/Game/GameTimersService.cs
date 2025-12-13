using Akka.Actor;

namespace GameService.Infrastructure;

public class GameTimersService(IActorRefFactory actorRefFactory) : Application.IGameTimersService
{
    public async Task StartTimerAsync(
        string gameId,
        int triggeredByEventNumber,
        string timerType,
        int timerGeneration,
        DateTime expiresAt,
        CancellationToken cancellationToken
    )
    {
        var selection = GetGameTimersActorSelection(gameId);
        await selection.Ask(
            new GameTimersActor.StartTimerCommand(triggeredByEventNumber, timerType, timerGeneration, expiresAt),
            cancellationToken
        );
    }

    public async Task CancelTimerAsync(
        string gameId,
        int triggeredByEventNumber,
        string timerType,
        int timerGeneration,
        CancellationToken cancellationToken
    )
    {
        var selection = GetGameTimersActorSelection(gameId);
        await selection.Ask(
            new GameTimersActor.CancelTimerCommand(triggeredByEventNumber, timerType, timerGeneration),
            cancellationToken
        );
    }

    private ActorSelection GetGameTimersActorSelection(string gameId)
    {
        return actorRefFactory.ActorSelection($"/user/game-{gameId}/game-timers");
    }
}
