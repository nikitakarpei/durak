using System.Text.Json;

namespace GameService.Application;

public class FoolDeclaredEventHandler(EventStore.IEventStore eventStore, IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.FoolDeclared>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.FoolDeclared> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.FoolDeclared),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);

        gameCollection.Delete(notification.GameId);
    }
}
