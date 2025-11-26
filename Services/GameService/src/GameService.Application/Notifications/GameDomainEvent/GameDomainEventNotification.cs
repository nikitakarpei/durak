namespace GameService.Application;

public record GameDomainEventNotification<TDomainEvent>(
    string GameId,
    Guid EventId,
    int EventNumber,
    TDomainEvent DomainEvent
) : MediatR.INotification;
