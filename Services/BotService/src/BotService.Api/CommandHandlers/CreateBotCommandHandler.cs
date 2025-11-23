namespace BotService.Api.CommandHandlers;

public class CreateBotCommandHandler(
    Application.PlayerIdentityServiceIntegration.IRpcClientFactory playerIdentityServiceClientFactory,
    Application.GameServiceIntegration.IRpcClientFactory gameServiceClientFactory,
    Application.Bot.IBotCreator botCreator
) : Rpc.Server.IUnaryCallHandler<Commands.CreateBotCommand.Types.Request, Commands.CreateBotCommand.Types.Response>
{
    public async Task<Commands.CreateBotCommand.Types.Response> HandleAsync(
        Commands.CreateBotCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        var (playerId, playerIdentityToken) = await LoginAsBot(request.BotId, cancellationToken);

        botCreator.Create(
            gameServiceClientFactory.Create(playerIdentityToken),
            new Domain.GameStateProjector(),
            CreateMoveChooser(request.BotType),
            request.GameId,
            playerId
        );

        return new Commands.CreateBotCommand.Types.Response();
    }

    private async Task<(string PlayerId, string PlayerIdentityToken)> LoginAsBot(
        string botId,
        CancellationToken cancellationToken
    )
    {
        using var playerIdentityServiceClient = playerIdentityServiceClientFactory.Create();

        var response = await playerIdentityServiceClient.UnaryCall<
            PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request,
            PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Response
        >(
            "LoginWithCustomId",
            new PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request { CustomId = $"bot-{botId}" },
            cancellationToken
        );

        return (response.Result.PlayerId, response.Result.PlayerIdToken);
    }

    private static Domain.Interfaces.IMoveChooser CreateMoveChooser(Google.Protobuf.WellKnownTypes.Any botType)
    {
        if (botType.Is(BotTypes.LowestValueBot.Descriptor))
        {
            return new Domain.LowestValueMoveChooser();
        }

        if (botType.Is(BotTypes.RandomBot.Descriptor))
        {
            return new Domain.RandomMoveChooser(new Random());
        }

        throw new ArgumentException($"Unknown bot type: {botType.TypeUrl}");
    }
}
