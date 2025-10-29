using Microsoft.Extensions.DependencyInjection;

namespace BotService.Application.Actors;

public partial class BotActor : Akka.Actor.ReceiveActor, Akka.Actor.ILogReceive
{
    private readonly IServiceScope _scope;
    private readonly GameIntegration.IGameServiceClientFactory _gameServiceClientFactory;

    private readonly string _gameId;
    private readonly Domain.Interfaces.IGameEventsProjector<Domain.GameState> _gameStateProjector;
    private readonly Domain.Interfaces.IMoveChooser _moveChooser;
    private readonly string _playerId;
    private readonly string _playerIdToken;
    private Domain.GameState _gameState = new();

    private readonly Rpc.Client.IClient _gameClient;
    private string? _gameAccessToken;

    public BotActor(
        IServiceProvider serviceProvider,
        string gameId,
        string playerId,
        string playerIdToken,
        Domain.Interfaces.IGameEventsProjector<Domain.GameState> gameEventsProjector,
        Domain.Interfaces.IMoveChooser moveChooser
    )
    {
        _scope = serviceProvider.CreateScope();
        _gameServiceClientFactory =
            _scope.ServiceProvider.GetRequiredService<GameIntegration.IGameServiceClientFactory>();

        _gameId = gameId;
        _gameStateProjector = gameEventsProjector;
        _moveChooser = moveChooser;
        _playerId = playerId;
        _playerIdToken = playerIdToken;

        ReceiveAsync<ProcessConnectedMessage>(HandleMessage);
        Receive<ProcessDisconnectedMessage>(HandleMessage);
        Receive<ProcessDomainEventMessage>(HandleMessage);

        var self = Self;
        _gameClient = _gameServiceClientFactory.Create(() => OnConnected(self), () => OnDisconnected(self));
    }

    protected override void PostStop()
    {
        _gameClient.Dispose();

        _scope.Dispose();
    }

    public record struct ProcessConnectedMessage();

    public record struct ProcessDisconnectedMessage();

    public record struct ProcessDomainEventMessage(object DomainEvent);

    private static void OnConnected(Akka.Actor.IActorRef self)
    {
        Akka.Actor.ActorRefImplicitSenderExtensions.Tell(self, new ProcessConnectedMessage());
    }

    private static void OnDisconnected(Akka.Actor.IActorRef self)
    {
        Akka.Actor.ActorRefImplicitSenderExtensions.Tell(self, new ProcessDisconnectedMessage());
    }

    private async Task HandleMessage(ProcessConnectedMessage message)
    {
        try
        {
            await EnterGame();
        }
        catch (Exception e)
        {
            Sender.Tell(new Akka.Actor.Status.Failure(e), Self);
        }
    }

    private void HandleMessage(ProcessDisconnectedMessage message) { }

    private void HandleMessage(ProcessDomainEventMessage message)
    {
        try
        {
            _gameState = _gameStateProjector.Apply(_gameState, message.DomainEvent);

            if (message.DomainEvent is Domain.GameEvents.YourTurnStarted yourTurnStarted)
            {
                Act(yourTurnStarted.PossibleMoves);
            }
        }
        catch (Exception e)
        {
            Sender.Tell(new Akka.Actor.Status.Failure(e), Self);
        }
    }

    private async Task EnterGame()
    {
        await GetGameAccessToken();
        await SubscribeToGameEvents();
        await JoinLobby();
    }

    private async Task GetGameAccessToken()
    {
        var request = new GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request
        {
            GameId = _gameId,
            PlayerIdToken = _playerIdToken,
        };

        var response = await _gameClient.UnaryCall<
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request,
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Response
        >(nameof(GameService.Api.Commands.GetGameAccessTokenCommand), request);

        if (
            response.ResultCase
            == GameService.Api.Commands.GetGameAccessTokenCommand.Types.Response.ResultOneofCase.Success
        )
        {
            _gameAccessToken = response.Success.AccessToken;
        }
        else
        {
            throw new Exception($"[GetGameAccessToken] Unexpected response: {response}");
        }
    }

    private Task SubscribeToGameEvents()
    {
        var request = new GameService.Api.Commands.GetGameEventsCommand.Types.Request
        {
            AccessToken = _gameAccessToken,
        };

        var responseStream = _gameClient.StreamingCall<
            GameService.Api.Commands.GetGameEventsCommand.Types.Request,
            GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse
        >(nameof(GameService.Api.Commands.GetGameEventsCommand), request);

        var self = Self;
        Task.Run(async () => await ProcessGameEventsStream(self, responseStream));

        return Task.CompletedTask;
    }

    private static async Task ProcessGameEventsStream(
        Akka.Actor.IActorRef self,
        IAsyncEnumerable<GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse> responseStream
    )
    {
        try
        {
            await foreach (var response in responseStream)
            {
                if (
                    response.ResultCase
                    != GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.NewEvent
                )
                {
                    throw new Exception($"Unexpected response: {response}");
                }

                var domainEvent = ApiExtensions.GameEventConverters.ConvertToDomainEvent(response.NewEvent.Event);

                Akka.Actor.ActorRefImplicitSenderExtensions.Tell(self, new ProcessDomainEventMessage(domainEvent));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[GetGameEvents] Unexpected error: {e}");

            // todo: stop actor
        }
    }

    private async Task JoinLobby()
    {
        var request = new GameService.Api.Commands.JoinLobbyCommand.Types.Request { AccessToken = _gameAccessToken };

        var response = await _gameClient.UnaryCall<
            GameService.Api.Commands.JoinLobbyCommand.Types.Request,
            GameService.Api.Commands.JoinLobbyCommand.Types.Response
        >(nameof(GameService.Api.Commands.JoinLobbyCommand), request);

        if (response.ResultCase != GameService.Api.Commands.JoinLobbyCommand.Types.Response.ResultOneofCase.Success)
        {
            throw new Exception($"[JoinLobby] Unexpected response: {response}");
        }
    }

    private void Act(Domain.PossibleMove[] possibleMoves)
    {
        var move = _moveChooser.ChooseMove(possibleMoves, [_gameState]);

        Task callTask = move switch
        {
            Domain.PossibleMoves.GiveUpDefence => _gameClient.UnaryCall<
                GameService.Api.Commands.GiveUpDefenceCommand.Types.Request,
                GameService.Api.Commands.GiveUpDefenceCommand.Types.Response
            >(
                nameof(GameService.Api.Commands.GiveUpDefenceCommand),
                new GameService.Api.Commands.GiveUpDefenceCommand.Types.Request { AccessToken = _gameAccessToken! }
            ),
            Domain.PossibleMoves.PlayAttackCard playAttackCard => _gameClient.UnaryCall<
                GameService.Api.Commands.PlayAttackCardCommand.Types.Request,
                GameService.Api.Commands.PlayAttackCardCommand.Types.Response
            >(
                nameof(GameService.Api.Commands.PlayAttackCardCommand),
                new GameService.Api.Commands.PlayAttackCardCommand.Types.Request
                {
                    AccessToken = _gameAccessToken!,
                    CardId = playAttackCard.YourCardId,
                }
            ),
            Domain.PossibleMoves.BeatAttackCard beatAttackCard => _gameClient.UnaryCall<
                GameService.Api.Commands.BeatAttackCardCommand.Types.Request,
                GameService.Api.Commands.BeatAttackCardCommand.Types.Response
            >(
                nameof(GameService.Api.Commands.BeatAttackCardCommand),
                new GameService.Api.Commands.BeatAttackCardCommand.Types.Request
                {
                    AccessToken = _gameAccessToken!,
                    AttackerCardId = beatAttackCard.TargetCardId,
                    DefenderCardId = beatAttackCard.YourCardId,
                }
            ),
            _ => throw new NotImplementedException(),
        };

        callTask.ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Console.WriteLine($"[Act] Unexpected error: {task.Exception}");

                // todo: stop actor
            }
        });
    }
}
