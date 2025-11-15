namespace GameService.Application;

public record GameDomainEventNotification<TDomainEvent>(
    string GameId,
    Domain.GameState OldState,
    Domain.GameState NewState,
    TDomainEvent DomainEvent
) : MediatR.INotification
    where TDomainEvent : Domain.GameEvent;
