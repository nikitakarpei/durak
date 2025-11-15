using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace GameService.Infrastructure;

public class GameActor : ReceiveActor, ILogReceive
{
    private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
    private readonly IServiceProvider _serviceProvider;

    private readonly string _gameId;
    private Domain.GameState _state = new();

    public GameActor(IServiceProvider serviceProvider, string gameId)
    {
        _serviceProvider = serviceProvider;
        _gameId = gameId;

        ReceiveAsync<ExecuteCommandMessage>(HandleMessage);
    }

    // Message types
    public record struct ExecuteCommandMessage(Domain.GameCommand Command);

    private async Task HandleMessage(ExecuteCommandMessage message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var gameService = scope.ServiceProvider.GetRequiredService<Domain.GameService>();

            var oldState = _state;

            var @event = (Domain.GameEvent?)gameService.Decide(oldState, (dynamic)message.Command);

            if (@event is null)
            {
                return;
            }

            var newState = oldState.When(@event);

            var eventStore = scope.ServiceProvider.GetRequiredService<EventStore.IEventStore>();

            await PersistEvent(eventStore, @event, CancellationToken.None);

            var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

            await mediator.Publish(CreateDomainEventNotification(oldState, newState, @event));

            _state = newState;
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private async Task PersistEvent(
        EventStore.IEventStore eventStore,
        object @event,
        CancellationToken cancellationToken
    )
    {
        var eventType = @event.GetType();
        var eventData = new EventStore.EventData(
            Guid.NewGuid(),
            eventType.Name,
            JsonSerializer.Serialize(@event, eventType)
        );
        var streamName = Application.GameEventStreams.DomainEvents(_gameId);
        await eventStore.AppendToStreamAsync(streamName, new[] { eventData }, cancellationToken);
    }

    private MediatR.INotification CreateDomainEventNotification(
        Domain.GameState oldState,
        Domain.GameState newState,
        object @event
    )
    {
        var eventType = @event.GetType();
        var notificationType = typeof(Application.GameDomainEventNotification<>).MakeGenericType(eventType);

        var notification =
            Activator.CreateInstance(notificationType, _gameId, oldState, newState, @event)
            ?? throw new InvalidOperationException($"Failed to create notification for event: {eventType.Name}");

        return (MediatR.INotification)notification;
    }
}
