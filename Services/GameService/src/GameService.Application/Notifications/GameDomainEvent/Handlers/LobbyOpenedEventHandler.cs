using System.Text.Json;

namespace GameService.Application;

public class LobbyOpenedEventHandler(EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.LobbyOpened>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.LobbyOpened> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.LobbyOpened),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);
    }
}
