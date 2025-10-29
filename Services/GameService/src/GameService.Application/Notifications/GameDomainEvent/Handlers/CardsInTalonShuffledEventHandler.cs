using System.Text.Json;

namespace GameService.Application;

public class CardsInTalonShuffledEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.CardsInTalonShuffled>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.CardsInTalonShuffled> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.CardsInTalonShuffled),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
