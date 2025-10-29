namespace GameService.Api;

public class JoinLobbyCommandHandler(
    Application.IGameCollection gameCollection,
    Application.IGameAccessTokenVerifier gameAccessTokenVerifier,
    Application.IVerifiedPlayerIdentityProvider verifiedPlayerIdentityProvider
) : Rpc.Server.IUnaryCallHandler<Commands.JoinLobbyCommand.Types.Request, Commands.JoinLobbyCommand.Types.Response>
{
    public async Task<Commands.JoinLobbyCommand.Types.Response> HandleAsync(
        Commands.JoinLobbyCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        var playerIdentity =
            verifiedPlayerIdentityProvider.Identity ?? throw new InvalidOperationException("Player identity not found");

        var gameAccessToken = await gameAccessTokenVerifier.VerifyAsync(request.AccessToken, cancellationToken);
        if (gameAccessToken is null)
        {
            return new Commands.JoinLobbyCommand.Types.Response()
            {
                InvalidAccessToken = new Commands.JoinLobbyCommand.Types.Response.Types.InvalidAccessToken(),
            };
        }

        var game = gameCollection.Get(gameAccessToken.GameId);
        game.When(new Domain.GameCommands.JoinLobby(playerIdentity.PlayerId));

        return new Commands.JoinLobbyCommand.Types.Response()
        {
            Success = new Commands.JoinLobbyCommand.Types.Response.Types.Success(),
        };
    }
}
