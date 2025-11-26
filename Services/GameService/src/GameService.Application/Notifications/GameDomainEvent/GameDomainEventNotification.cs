namespace GameService.Application;

public record GameDomainEventNotification<TDomainEvent>(string GameId, Guid EventId, TDomainEvent DomainEvent)
    : MediatR.INotification;
