using System.Text.Json;

namespace GameService.Application;

public class AllPlayersJoinedEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.AllPlayersJoined>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.AllPlayersJoined> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.AllPlayersJoined),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
