using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace GameService.Infrastructure;

public class GameActor : ReceiveActor, ILogReceive
{
    private readonly IServiceScope _scope;
    private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
    private readonly Domain.GameService _gameService;
    private readonly EventStore.IEventStore _eventStore;
    private readonly MediatR.IMediator _mediator;

    private readonly string _gameId;
    private readonly List<object> _events = [];

    public GameActor(IServiceProvider serviceProvider, string gameId)
    {
        _scope = serviceProvider.CreateScope();
        _gameService = _scope.ServiceProvider.GetRequiredService<Domain.GameService>();
        _eventStore = _scope.ServiceProvider.GetRequiredService<EventStore.IEventStore>();
        _mediator = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        _gameId = gameId;

        ReceiveAsync<ExecuteCommandMessage>(HandleMessage);
    }

    protected override void PostStop()
    {
        _scope.Dispose();
    }

    // Message types
    public record struct ExecuteCommandMessage(Domain.GameCommand Command);

    private async Task HandleMessage(ExecuteCommandMessage message)
    {
        try
        {
            var game = new Domain.GameAggregate();

            game.Load(_events);

            _gameService.HandleCommand(game, (dynamic)message.Command);

            ProcessEvents(game, _gameService);

            await PersistEvents(game.Changes, CancellationToken.None);

            // FIXME: implement at-least-once processing of events
            foreach (var @event in game.Changes)
            {
                await _mediator.Publish(CreateDomainEventNotification(@event));
            }

            _events.AddRange(game.Changes);
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

            foreach (var @event in eventsToProcess)
            {
                gameService.HandleEvent(game, (dynamic)@event);
            }

            processedEventCount = game.Changes.Count;
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

    private MediatR.INotification CreateDomainEventNotification(object @event)
    {
        var eventType = @event.GetType();
        var notificationType = typeof(Application.GameDomainEventNotification<>).MakeGenericType(eventType);

        var notification =
            Activator.CreateInstance(notificationType, _gameId, @event)
            ?? throw new InvalidOperationException($"Failed to create notification for event: {eventType.Name}");

        return (MediatR.INotification)notification;
    }
}
