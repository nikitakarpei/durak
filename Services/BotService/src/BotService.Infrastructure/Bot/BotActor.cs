using Akka.Actor;
using BotService.Api.ApiExtensions;
using Microsoft.Extensions.Logging;

namespace BotService.Infrastructure.Bot;

public partial class BotActor : ReceiveActor, ILogReceive
{
    private readonly ILogger<BotActor> _logger;
    private readonly Rpc.Client.IClient _gameClient;
    private readonly Domain.Interfaces.IGameEventsProjector<Domain.GameState> _gameStateProjector;
    private readonly Domain.Interfaces.IMoveChooser _moveChooser;
    private readonly string _gameId;
    private readonly string _playerId;

    private Domain.GameState _gameState = new();
    private string? _gameAccessToken;

    public BotActor(
        ILogger<BotActor> logger,
        Rpc.Client.IClient gameClient,
        Domain.Interfaces.IGameEventsProjector<Domain.GameState> gameStateProjector,
        Domain.Interfaces.IMoveChooser moveChooser,
        string gameId,
        string playerId
    )
    {
        _logger = logger;
        _gameClient = gameClient;
        _gameStateProjector = gameStateProjector;
        _moveChooser = moveChooser;
        _gameId = gameId;
        _playerId = playerId;

        ReceiveAsync<EnterGameMessage>(HandleMessage);
        Receive<ProcessOldDomainEventMessage>(HandleMessage);
        Receive<ProcessNewDomainEventMessage>(HandleMessage);
    }

    protected override void PostStop()
    {
        _gameClient.Dispose();
    }

    public record struct EnterGameMessage();

    public record struct ProcessOldDomainEventMessage(object DomainEvent);

    public record struct ProcessNewDomainEventMessage(object DomainEvent);

    private async Task HandleMessage(EnterGameMessage message)
    {
        try
        {
            await EnterGame();
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleMessage(ProcessOldDomainEventMessage message)
    {
        try
        {
            _gameState = _gameStateProjector.Apply(_gameState, message.DomainEvent);
        }
        catch (Exception e)
        {
            Sender.Tell(new Status.Failure(e), Self);
        }
    }

    private void HandleMessage(ProcessNewDomainEventMessage message)
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
            Sender.Tell(new Status.Failure(e), Self);
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
        var request = new GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request { GameId = _gameId };

        var response = await _gameClient.UnaryCall<
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Request,
            GameService.Api.Commands.GetGameAccessTokenCommand.Types.Response
        >("GetGameAccessToken", request);

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
        var request = new GameService.Api.Commands.GetGameEventsCommand.Types.Request { GameId = _gameId };

        var responseStream = _gameClient.StreamingCall<
            GameService.Api.Commands.GetGameEventsCommand.Types.Request,
            GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse
        >("GetGameEvents", request);

        var self = Self;
        Task.Run(async () => await ProcessGameEventsStream(_logger, self, responseStream));

        return Task.CompletedTask;
    }

    private static async Task ProcessGameEventsStream(
        ILogger<BotActor> logger,
        IActorRef self,
        IAsyncEnumerable<GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse> responseStream
    )
    {
        try
        {
            await foreach (var response in responseStream)
            {
                switch (response.ResultCase)
                {
                    case GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.OldEvent:
                    {
                        var domainEvent = GameEventConverters.ConvertToDomainEvent(response.OldEvent.Event);
                        self.Tell(new ProcessOldDomainEventMessage(domainEvent));
                        break;
                    }
                    case GameService
                        .Api
                        .Commands
                        .GetGameEventsCommand
                        .Types
                        .StreamResponse
                        .ResultOneofCase
                        .OldEventsEnded:
                    {
                        break;
                    }
                    case GameService.Api.Commands.GetGameEventsCommand.Types.StreamResponse.ResultOneofCase.NewEvent:
                    {
                        var domainEvent = GameEventConverters.ConvertToDomainEvent(response.NewEvent.Event);
                        self.Tell(new ProcessNewDomainEventMessage(domainEvent));
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Unexpected response: {response}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "[GetGameEvents] Unexpected error");
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
        >("JoinLobby", request);

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
                "GiveUpDefence",
                new GameService.Api.Commands.GiveUpDefenceCommand.Types.Request { AccessToken = _gameAccessToken! }
            ),
            Domain.PossibleMoves.PlayAttackCard playAttackCard => _gameClient.UnaryCall<
                GameService.Api.Commands.PlayAttackCardCommand.Types.Request,
                GameService.Api.Commands.PlayAttackCardCommand.Types.Response
            >(
                "PlayAttackCard",
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
                "BeatAttackCard",
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
