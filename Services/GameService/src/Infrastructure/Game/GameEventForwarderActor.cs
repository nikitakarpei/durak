using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GameService.Infrastructure;

public class GameEventForwarderActor : ReceivePersistentActor, ILogReceive
{
    public override string PersistenceId => $"game-event-forwarder-{_gameId}";

    private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
    private readonly IServiceScope _scope;
    private readonly EventStore.IEventStore _eventStore;
    private readonly MediatR.IMediator _mediator;
    private readonly string _gameId;

    private Task _eventSubscriptionTask = Task.CompletedTask;
    private CancellationTokenSource _eventSubscriptionCancellationTokenSource = new();
    private int _processedEventsCount = 0;

    private static readonly Dictionary<string, Type> EventTypeCache = typeof(Domain.GameEvents)
        .GetNestedTypes()
        .Where(t => t.IsClass || t.IsValueType)
        .ToDictionary(t => t.Name, t => t);

    public GameEventForwarderActor(IServiceProvider serviceProvider, string gameId)
    {
        _scope = serviceProvider.CreateScope();
        _eventStore = _scope.ServiceProvider.GetRequiredService<EventStore.IEventStore>();
        _mediator = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        _gameId = gameId;

        CommandAsync<ProcessEventMessage>(HandleCommand);

        Recover<EventProcessedEvent>(HandleRecover);
        Recover<RecoveryCompleted>(HandleRecover);
    }

    protected override void PostStop()
    {
        _eventSubscriptionCancellationTokenSource.Cancel();
        _eventSubscriptionTask.Wait();
        _scope.Dispose();
    }

    public record struct ProcessEventMessage(EventStore.RecordedEvent Event, int EventNumber);

    public record struct EventProcessedEvent();

    private async Task HandleCommand(ProcessEventMessage message)
    {
        var domainEvent = DeserializeDomainEvent(message.Event);

        await _mediator.Publish(CreateDomainEventNotification(message.Event.EventId, message.EventNumber, domainEvent));

        Persist(
            new EventProcessedEvent(),
            @event =>
            {
                ++_processedEventsCount;
            }
        );
    }

    private void HandleRecover(EventProcessedEvent eventProcessedEvent)
    {
        ++_processedEventsCount;
    }

    private void HandleRecover(RecoveryCompleted recoveryCompleted)
    {
        var self = Self;
        var streamId = Application.GameEventStreams.DomainEvents(_gameId);
        var processedEventsCount = _processedEventsCount;
        var cancellationToken = _eventSubscriptionCancellationTokenSource.Token;

        _eventSubscriptionTask = SubscribeToEventsAsync(
            self,
            _eventStore,
            streamId,
            processedEventsCount,
            cancellationToken
        );

        _eventSubscriptionTask = _eventSubscriptionTask.ContinueWith(
            static (task, state) =>
            {
                if (task.IsFaulted)
                {
                    var (log, actorRef) = ((ILoggingAdapter, IActorRef))state!;
                    log.Error(task.Exception, "Event subscription failed");
                    actorRef.Tell(PoisonPill.Instance);
                }
            },
            (_log, self)
        );
    }

    private static object DeserializeDomainEvent(EventStore.RecordedEvent @event)
    {
        if (!EventTypeCache.TryGetValue(@event.Type, out var eventType))
        {
            throw new ArgumentException($"Unknown event type: {@event.Type}");
        }

        var domainEvent = JsonSerializer.Deserialize(@event.JsonData, eventType);

        if (domainEvent is null)
        {
            throw new Exception($"Failed to deserialize domain event: {@event.Type}");
        }

        return domainEvent;
    }

    private MediatR.INotification CreateDomainEventNotification(Guid eventId, int eventNumber, object @event)
    {
        var eventType = @event.GetType();
        var notificationType = typeof(Application.GameDomainEventNotification<>).MakeGenericType(eventType);

        var notification =
            Activator.CreateInstance(notificationType, _gameId, eventId, eventNumber, @event)
            ?? throw new InvalidOperationException($"Failed to create notification for event: {eventType.Name}");

        return (MediatR.INotification)notification;
    }

    private static async Task SubscribeToEventsAsync(
        IActorRef actorRef,
        EventStore.IEventStore eventStore,
        string streamId,
        int alreadyProcessedCount,
        CancellationToken cancellationToken
    )
    {
        var events = eventStore.SubscribeToStreamAsync(streamId, alreadyProcessedCount, cancellationToken);

        await foreach (var @event in events)
        {
            actorRef.Tell(new ProcessEventMessage(@event, @event.EventNumber));
        }
    }
}
