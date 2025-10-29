using System.Text.Json;

namespace GameService.Application;

public class AttackCardPlayedEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.AttackCardPlayed>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.AttackCardPlayed> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.AttackCardPlayed),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
