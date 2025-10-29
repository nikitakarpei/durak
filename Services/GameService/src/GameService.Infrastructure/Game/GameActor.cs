using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace GameService.Infrastructure;

public class GameActor : ReceiveActor, ILogReceive
{
    private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
    private readonly IServiceScope _scope;
    private readonly MediatR.IMediator _mediator;

    private readonly string _gameId;
    private readonly Domain.GameAggregate _gameAggregate;

    public GameActor(IServiceProvider serviceProvider, string gameId, int seed, Domain.GameOption[] gameOptions)
    {
        _scope = serviceProvider.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();

        _gameId = gameId;
        _gameAggregate = Domain.GameAggregate.Create(new Random(seed), gameOptions, HandleDomainEvent);

        Receive<OpenLobbyMessage>(HandleMessage);
        Receive<JoinLobbyMessage>(HandleMessage);
        Receive<PlayAttackCardMessage>(HandleMessage);
        Receive<BeatAttackCardMessage>(HandleMessage);
        Receive<GiveUpDefenceMessage>(HandleMessage);
        ReceiveAsync<PublishDomainEventMessage>(HandleMessage);
    }

    protected override void PostStop()
    {
        _scope.Dispose();
    }

    // Message types
    public record struct OpenLobbyMessage();

    public record struct JoinLobbyMessage(string PlayerId);

    public record struct PlayAttackCardMessage(string PlayerId, string CardId);

    public record struct BeatAttackCardMessage(string PlayerId, string AttackerCardId, string DefenderCardId);

    public record struct GiveUpDefenceMessage(string PlayerId);

    private record struct PublishDomainEventMessage(object Event);

    private void HandleMessage(OpenLobbyMessage message)
    {
        try
        {
            _gameAggregate.When(new Domain.GameCommands.OpenLobby());
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleMessage(JoinLobbyMessage message)
    {
        try
        {
            _gameAggregate.When(new Domain.GameCommands.JoinLobby(message.PlayerId));
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleMessage(PlayAttackCardMessage message)
    {
        try
        {
            _gameAggregate.When(new Domain.GameCommands.PlayAttackCard(message.PlayerId, message.CardId));
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleMessage(BeatAttackCardMessage message)
    {
        try
        {
            _gameAggregate.When(
                new Domain.GameCommands.BeatAttackCard(message.PlayerId, message.AttackerCardId, message.DefenderCardId)
            );
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleMessage(GiveUpDefenceMessage message)
    {
        try
        {
            _gameAggregate.When(new Domain.GameCommands.GiveUpDefence(message.PlayerId));
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private async Task HandleMessage(PublishDomainEventMessage message)
    {
        try
        {
            await _mediator.Publish(CreateDomainEventNotification(message.Event));
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private MediatR.INotification CreateDomainEventNotification(object @event)
    {
        return @event switch
        {
            Domain.GameEvents.ActivePlayerChanged activePlayerChanged =>
                new Application.GameDomainEventNotification<Domain.GameEvents.ActivePlayerChanged>(
                    _gameId,
                    activePlayerChanged
                ),
            Domain.GameEvents.AllPlayersJoined allPlayersJoined =>
                new Application.GameDomainEventNotification<Domain.GameEvents.AllPlayersJoined>(
                    _gameId,
                    allPlayersJoined
                ),
            Domain.GameEvents.AttackCardBeaten attackCardBeaten =>
                new Application.GameDomainEventNotification<Domain.GameEvents.AttackCardBeaten>(
                    _gameId,
                    attackCardBeaten
                ),
            Domain.GameEvents.AttackCardPlayed attackCardPlayed =>
                new Application.GameDomainEventNotification<Domain.GameEvents.AttackCardPlayed>(
                    _gameId,
                    attackCardPlayed
                ),
            Domain.GameEvents.CardDealtToPlayer cardDealtToPlayer =>
                new Application.GameDomainEventNotification<Domain.GameEvents.CardDealtToPlayer>(
                    _gameId,
                    cardDealtToPlayer
                ),
            Domain.GameEvents.CardDiscarded cardDiscarded =>
                new Application.GameDomainEventNotification<Domain.GameEvents.CardDiscarded>(_gameId, cardDiscarded),
            Domain.GameEvents.CardsInTalonShuffled cardsInTalonShuffled =>
                new Application.GameDomainEventNotification<Domain.GameEvents.CardsInTalonShuffled>(
                    _gameId,
                    cardsInTalonShuffled
                ),
            Domain.GameEvents.CommonTrumpCardChosen commonTrumpCardChosen =>
                new Application.GameDomainEventNotification<Domain.GameEvents.CommonTrumpCardChosen>(
                    _gameId,
                    commonTrumpCardChosen
                ),
            Domain.GameEvents.DefenceGivenUp defenceGivenUp =>
                new Application.GameDomainEventNotification<Domain.GameEvents.DefenceGivenUp>(_gameId, defenceGivenUp),
            Domain.GameEvents.FoolDeclared foolDeclared =>
                new Application.GameDomainEventNotification<Domain.GameEvents.FoolDeclared>(_gameId, foolDeclared),
            Domain.GameEvents.GameCreated gameCreated =>
                new Application.GameDomainEventNotification<Domain.GameEvents.GameCreated>(_gameId, gameCreated),
            Domain.GameEvents.LobbyOpened lobbyOpened =>
                new Application.GameDomainEventNotification<Domain.GameEvents.LobbyOpened>(_gameId, lobbyOpened),
            Domain.GameEvents.PlayerFinished playerFinished =>
                new Application.GameDomainEventNotification<Domain.GameEvents.PlayerFinished>(_gameId, playerFinished),
            Domain.GameEvents.PlayerJoined playerJoined =>
                new Application.GameDomainEventNotification<Domain.GameEvents.PlayerJoined>(_gameId, playerJoined),
            Domain.GameEvents.RoundFinished roundFinished =>
                new Application.GameDomainEventNotification<Domain.GameEvents.RoundFinished>(_gameId, roundFinished),
            Domain.GameEvents.RoundStarted roundStarted =>
                new Application.GameDomainEventNotification<Domain.GameEvents.RoundStarted>(_gameId, roundStarted),
            _ => throw new InvalidOperationException($"Unknown event: {@event.GetType().Name}"),
        };
    }

    private void HandleDomainEvent(object @event)
    {
        Self.Tell(new PublishDomainEventMessage(@event), Self);
    }
}
