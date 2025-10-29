namespace GameService.Application;

public record GameDomainEventNotification<TDomainEvent>(string GameId, TDomainEvent DomainEvent)
    : MediatR.INotification;
