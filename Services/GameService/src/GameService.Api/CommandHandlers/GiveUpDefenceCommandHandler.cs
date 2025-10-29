namespace GameService.Api;

public class GiveUpDefenceCommandHandler(
    Application.IGameCollection gameCollection,
    Application.IGameAccessTokenVerifier gameAccessTokenVerifier,
    Application.IVerifiedPlayerIdentityProvider verifiedPlayerIdentityProvider
)
    : Rpc.Server.IUnaryCallHandler<
        Commands.GiveUpDefenceCommand.Types.Request,
        Commands.GiveUpDefenceCommand.Types.Response
    >
{
    public async Task<Commands.GiveUpDefenceCommand.Types.Response> HandleAsync(
        Commands.GiveUpDefenceCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        var playerIdentity =
            verifiedPlayerIdentityProvider.Identity ?? throw new InvalidOperationException("Player identity not found");

        var gameAccessToken = await gameAccessTokenVerifier.VerifyAsync(request.AccessToken, cancellationToken);
        if (gameAccessToken is null)
        {
            return new Commands.GiveUpDefenceCommand.Types.Response()
            {
                InvalidAccessToken = new Commands.GiveUpDefenceCommand.Types.Response.Types.InvalidAccessToken(),
            };
        }

        var game = gameCollection.Get(gameAccessToken.GameId);
        game.When(new Domain.GameCommands.GiveUpDefence(playerIdentity.PlayerId));

        return new Commands.GiveUpDefenceCommand.Types.Response()
        {
            Success = new Commands.GiveUpDefenceCommand.Types.Response.Types.Success(),
        };
    }
}
