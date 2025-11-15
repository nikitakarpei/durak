using System.Security.Cryptography;

namespace GameService.Api;

public class CreateGameCommandHandler(Application.IGameCollection gameCollection)
    : Rpc.Server.IUnaryCallHandler<Commands.CreateGameCommand.Types.Request, Commands.CreateGameCommand.Types.Response>
{
    public Task<Commands.CreateGameCommand.Types.Response> HandleAsync(
        Commands.CreateGameCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        // todo: check signature

        var gameOptions = request.ParseGameOptions();

        var gameId = Guid.NewGuid().ToString();

        gameCollection.Create(gameId, gameOptions, RandomNumberGenerator.GetBytes(32));

        var game = gameCollection.Get(gameId);
        game.Execute(new Domain.GameCommands.OpenLobby());

        return Task.FromResult(
            new Commands.CreateGameCommand.Types.Response
            {
                Success = new Commands.CreateGameCommand.Types.Response.Types.Success() { GameId = gameId },
            }
        );
    }
}
