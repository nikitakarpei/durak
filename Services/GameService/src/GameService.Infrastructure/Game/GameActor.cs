using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameService.Infrastructure;

public class GameActor : ReceivePersistentActor, ILogReceive
{
    public override string PersistenceId => $"game-{_gameId}";

    private readonly IServiceScope _scope;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
    private readonly Domain.GameService _gameService;
    private readonly EventStore.IEventStore _eventStore;

    private readonly string _gameId;
    private readonly List<object> _events = [];

    public GameActor(IServiceProvider serviceProvider, string gameId)
    {
        _scope = serviceProvider.CreateScope();
        _rootServiceProvider = serviceProvider;
        _gameService = _scope.ServiceProvider.GetRequiredService<Domain.GameService>();
        _eventStore = _scope.ServiceProvider.GetRequiredService<EventStore.IEventStore>();
        _gameId = gameId;

        CommandAsync<ExecuteCommandMessage>(HandleCommand);
    }

    protected override void PreStart()
    {
        Context.ActorOf(Props.Create<GameEventForwarderActor>(_rootServiceProvider, _gameId), "game-event-forwarder");
        Context.ActorOf(Props.Create<GameTimersActor>(_rootServiceProvider, _gameId), "game-timers");
    }

    protected override void PostStop()
    {
        _scope.Dispose();
    }

    // Message types
    public record struct ExecuteCommandMessage(Domain.GameCommand Command);

    public record struct ProcessEventMessage(EventStore.RecordedEvent Event);

    private async Task HandleCommand(ExecuteCommandMessage message)
    {
        try
        {
            var game = new Domain.GameAggregate();

            game.Load(_events);

            _gameService.HandleCommand(game, (dynamic)message.Command);

            ProcessEvents(game, _gameService);

            await PersistEvents(game.Changes, CancellationToken.None);

            _events.AddRange(game.Changes);

            Sender.Tell(new Status.Success(true), Self);
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private static void ProcessEvents(Domain.GameAggregate game, Domain.GameService gameService)
    {
        var processedEventCount = 0;
        const int maxIterations = 100; // Safety limit to prevent infinite loops
        var iteration = 0;

        while (processedEventCount < game.Changes.Count)
        {
            if (++iteration > maxIterations)
            {
                var eventNames = string.Join(", ", game.Changes.Select(e => e.GetType().Name));
                throw new InvalidOperationException(
                    $"Event processing exceeded {maxIterations} iterations - possible infinite loop. "
                        + $"Events processed: {eventNames}"
                );
            }

            var eventsToProcess = game.Changes.Skip(processedEventCount).ToList();
            processedEventCount = game.Changes.Count;

            foreach (var @event in eventsToProcess)
            {
                gameService.HandleEvent(game, (dynamic)@event);
            }
        }
    }

    private async Task PersistEvents(IEnumerable<object> events, CancellationToken cancellationToken)
    {
        var streamName = Application.GameEventStreams.DomainEvents(_gameId);
        var eventDatas = events.Select(@event => new EventStore.EventData(
            Guid.NewGuid(),
            @event.GetType().Name,
            JsonSerializer.Serialize(@event, @event.GetType())
        ));

        await _eventStore.AppendToStreamAsync(streamName, eventDatas, cancellationToken);
    }
}
