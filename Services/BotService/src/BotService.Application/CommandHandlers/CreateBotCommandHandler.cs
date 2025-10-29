using BotService.Application.Actors;

namespace BotService.Application.CommandHandlers;

public class CreateBotCommandHandler
{
    private readonly Akka.Actor.IActorRefFactory _actorRefFactory;
    private readonly Akka.DependencyInjection.IDependencyResolver _dependencyResolver;

    public CreateBotCommandHandler(
        Akka.Actor.IActorRefFactory actorRefFactory,
        Akka.DependencyInjection.IDependencyResolver dependencyResolver
    )
    {
        _actorRefFactory = actorRefFactory;
        _dependencyResolver = dependencyResolver;
    }

    public Task<Api.Commands.CreateBotCommand.Types.Response> Handle(
        Api.Commands.CreateBotCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        var playerId = Guid.NewGuid().ToString();
        var playerSecret = Guid.NewGuid().ToString();

        var gameStateProjector = new Domain.GameStateProjector();
        var moveChooser = CreateMoveChooser(request.BotType);

        var props = _dependencyResolver.Props<BotActor>(
            request.GameId,
            playerId,
            playerSecret,
            gameStateProjector,
            moveChooser
        );

        _actorRefFactory.ActorOf(props, playerId);

        return Task.FromResult(new Api.Commands.CreateBotCommand.Types.Response());
    }

    private static Domain.Interfaces.IMoveChooser CreateMoveChooser(Google.Protobuf.WellKnownTypes.Any botType)
    {
        if (botType.Is(Api.BotTypes.LowestValueBot.Descriptor))
        {
            return new Domain.LowestValueMoveChooser();
        }

        if (botType.Is(Api.BotTypes.RandomBot.Descriptor))
        {
            return new Domain.RandomMoveChooser(new Random());
        }

        throw new ArgumentException($"Unknown bot type: {botType.TypeUrl}");
    }
}
