using System.Text.Json;

namespace GameService.Application;

public class CardDealtToPlayerEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.CardDealtToPlayer>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.CardDealtToPlayer> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.CardDealtToPlayer),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
