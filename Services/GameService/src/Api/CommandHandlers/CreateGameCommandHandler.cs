using System.Security.Cryptography;

namespace GameService.Api;

public class CreateGameCommandHandler(Application.IGameCollection gameCollection)
    : Rpc.Server.IUnaryCallHandler<Commands.CreateGameCommand.Types.Request, Commands.CreateGameCommand.Types.Response>
{
    public async Task<Commands.CreateGameCommand.Types.Response> HandleAsync(
        Commands.CreateGameCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        // todo: check signature

        var gameOptions = request.ParseGameOptions();

        var gameId = Guid.NewGuid().ToString();

        await gameCollection.CreateAsync(gameId, gameOptions, RandomNumberGenerator.GetBytes(32), cancellationToken);

        return new Commands.CreateGameCommand.Types.Response
        {
            Success = new Commands.CreateGameCommand.Types.Response.Types.Success() { GameId = gameId },
        };
    }
}
