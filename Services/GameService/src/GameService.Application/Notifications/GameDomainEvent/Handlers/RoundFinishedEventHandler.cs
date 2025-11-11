using System.Text.Json;

namespace GameService.Application;

public class RoundFinishedEventHandler(IGameCollection gameCollection, EventStore.IEventStore eventStore)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.RoundFinished>>
{
    public async Task Handle(
        GameDomainEventNotification<Domain.GameEvents.RoundFinished> notification,
        CancellationToken cancellationToken
    )
    {
        var events = new List<EventStore.EventData>
        {
            new(
                Guid.NewGuid(),
                nameof(Domain.GameEvents.RoundFinished),
                JsonSerializer.Serialize(notification.DomainEvent)
            ),
        };

        var streamName = GameEventStreams.DomainEvents(notification.GameId);
        await eventStore.AppendToStreamAsync(streamName, events, cancellationToken);

        gameCollection.Get(notification.GameId).When(new Domain.GameCommands.StartNextRound());
    }
}
