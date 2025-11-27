using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameService.Infrastructure;

public class GameTimersActor : ReceivePersistentActor, ILogReceive, IWithTimers
{
    public ITimerScheduler Timers { get; set; } = null!;
    private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
    private readonly IServiceScope _scope;
    private readonly MediatR.IMediator _mediator;
    private readonly string _gameId;

    public override string PersistenceId => $"game-timers-{_gameId}";

    private record ActiveTimer(int TimerGeneration, DateTime ExpiresAt);

    private readonly Dictionary<string, ActiveTimer> _activeTimersByType = [];
    private int _lastTriggeredByEventNumber = -1;

    public GameTimersActor(IServiceProvider serviceProvider, string gameId)
    {
        _scope = serviceProvider.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        _gameId = gameId;

        Command<StartTimerCommand>(HandleCommand);
        Command<CancelTimerCommand>(HandleCommand);
        CommandAsync<HandleTimerExpiredCommand>(HandleCommand);

        Recover<TimerStartedEvent>(HandleRecover);
        Recover<TimerCancelledEvent>(HandleRecover);
        Recover<TimerExpiredEvent>(HandleRecover);
        Recover<RecoveryCompleted>(HandleRecover);
    }

    protected override void PostStop()
    {
        _scope.Dispose();
    }

    public record struct StartTimerCommand(
        int TriggeredByEventNumber,
        string TimerType,
        int TimerGeneration,
        DateTime ExpiresAt
    );

    public record struct CancelTimerCommand(int TriggeredByEventNumber, string TimerType, int TimerGeneration);

    private record struct HandleTimerExpiredCommand(string TimerType, int TimerGeneration);

    private record struct TimerStartedEvent(
        int TriggeredByEventNumber,
        string TimerType,
        int TimerGeneration,
        DateTime ExpiresAt
    );

    private record struct TimerCancelledEvent(int TriggeredByEventNumber, string TimerType, int TimerGeneration);

    private record struct TimerExpiredEvent(string TimerType);

    private void HandleCommand(StartTimerCommand command)
    {
        try
        {
            if (command.TriggeredByEventNumber <= _lastTriggeredByEventNumber)
            {
                Sender.Tell(new Status.Success(true), Self);
                return;
            }

            if (_activeTimersByType.ContainsKey(command.TimerType))
            {
                throw new InvalidOperationException($"Timer is already running for type {command.TimerType}");
            }

            StartTimer(command.TimerType, command.TimerGeneration, command.ExpiresAt);

            _lastTriggeredByEventNumber = command.TriggeredByEventNumber;
            _activeTimersByType.Add(command.TimerType, new ActiveTimer(command.TimerGeneration, command.ExpiresAt));

            var evt = new TimerStartedEvent(
                command.TriggeredByEventNumber,
                command.TimerType,
                command.TimerGeneration,
                command.ExpiresAt
            );
            Persist(
                evt,
                @event =>
                {
                    Sender.Tell(new Status.Success(true), Self);
                }
            );
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleCommand(CancelTimerCommand command)
    {
        try
        {
            if (command.TriggeredByEventNumber <= _lastTriggeredByEventNumber)
            {
                Sender.Tell(new Status.Success(true), Self);
                return;
            }

            if (!_activeTimersByType.TryGetValue(command.TimerType, out var activeTimer))
            {
                throw new InvalidOperationException($"Timer is not running for type {command.TimerType}");
            }

            Timers.Cancel(command.TimerType);

            _lastTriggeredByEventNumber = command.TriggeredByEventNumber;
            _activeTimersByType.Remove(command.TimerType);

            var evt = new TimerCancelledEvent(
                command.TriggeredByEventNumber,
                command.TimerType,
                command.TimerGeneration
            );
            Persist(
                evt,
                @event =>
                {
                    Sender.Tell(new Status.Success(true), Self);
                }
            );
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private async Task HandleCommand(HandleTimerExpiredCommand command)
    {
        try
        {
            if (!_activeTimersByType.TryGetValue(command.TimerType, out var activeTimer))
            {
                _log.Warning("Timer expired for type {TimerType} but no active timer found", command.TimerType);
                return;
            }

            if (activeTimer.TimerGeneration != command.TimerGeneration)
            {
                _log.Warning(
                    $"Timer expired for type {command.TimerType} but generation mismatch. Expected {command.TimerGeneration}, got {activeTimer.TimerGeneration}"
                );
                return;
            }

            await _mediator.Publish(
                new Application.GameTimerExpiredNotification(_gameId, command.TimerType, command.TimerGeneration)
            );

            _activeTimersByType.Remove(command.TimerType);

            var evt = new TimerExpiredEvent(command.TimerType);
            Persist(
                evt,
                @event =>
                {
                    Sender.Tell(new Status.Success(true), Self);
                }
            );
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleRecover(TimerStartedEvent evt)
    {
        _activeTimersByType.Add(evt.TimerType, new ActiveTimer(evt.TimerGeneration, evt.ExpiresAt));
        _lastTriggeredByEventNumber = evt.TriggeredByEventNumber;
    }

    private void HandleRecover(TimerCancelledEvent evt)
    {
        _activeTimersByType.Remove(evt.TimerType);
        _lastTriggeredByEventNumber = evt.TriggeredByEventNumber;
    }

    private void HandleRecover(TimerExpiredEvent evt)
    {
        _activeTimersByType.Remove(evt.TimerType);
    }

    private void HandleRecover(RecoveryCompleted recoveryCompleted)
    {
        foreach (var (timerType, activeTimer) in _activeTimersByType)
        {
            _log.Info(
                $"Recovering timer for type {timerType}, generation {activeTimer.TimerGeneration}, expires at {activeTimer.ExpiresAt}"
            );
            StartTimer(timerType, activeTimer.TimerGeneration, activeTimer.ExpiresAt);
        }
    }

    private void StartTimer(string TimerType, int TimerGeneration, DateTime ExpiresAt)
    {
        var remainingTime = ExpiresAt - DateTime.UtcNow;

        if (remainingTime <= TimeSpan.Zero)
        {
            // Timer already expired, handle immediately
            Self.Tell(new HandleTimerExpiredCommand(TimerType, TimerGeneration));
        }
        else
        {
            Timers.StartSingleTimer(
                TimerType,
                new HandleTimerExpiredCommand(TimerType, TimerGeneration),
                remainingTime
            );
        }
    }
}
