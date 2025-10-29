using System.Text.Json;

namespace GameService.Application;

public class DefenceGivenUpEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.DefenceGivenUp>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.DefenceGivenUp> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.DefenceGivenUp),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
