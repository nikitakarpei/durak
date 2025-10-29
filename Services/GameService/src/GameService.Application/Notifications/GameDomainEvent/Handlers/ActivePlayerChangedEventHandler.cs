using System.Text.Json;

namespace GameService.Application;

public class ActivePlayerChangedEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.ActivePlayerChanged>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.ActivePlayerChanged> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.ActivePlayerChanged),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
