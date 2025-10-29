using System.Text.Json;

namespace GameService.Application;

public class CommonTrumpCardChosenEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.CommonTrumpCardChosen>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.CommonTrumpCardChosen> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.CommonTrumpCardChosen),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
