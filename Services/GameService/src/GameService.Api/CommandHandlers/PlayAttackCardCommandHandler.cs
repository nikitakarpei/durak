namespace GameService.Api;

public class PlayAttackCardCommandHandler(
    Application.IGameCollection gameCollection,
    Application.IGameAccessTokenVerifier gameAccessTokenVerifier,
    Application.IVerifiedPlayerIdentityProvider verifiedPlayerIdentityProvider
)
    : Rpc.Server.IUnaryCallHandler<
        Commands.PlayAttackCardCommand.Types.Request,
        Commands.PlayAttackCardCommand.Types.Response
    >
{
    public async Task<Commands.PlayAttackCardCommand.Types.Response> HandleAsync(
        Commands.PlayAttackCardCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        var playerIdentity =
            verifiedPlayerIdentityProvider.Identity ?? throw new InvalidOperationException("Player identity not found");

        var gameAccessToken = await gameAccessTokenVerifier.VerifyAsync(request.AccessToken, cancellationToken);
        if (gameAccessToken is null)
        {
            return new Commands.PlayAttackCardCommand.Types.Response()
            {
                InvalidAccessToken = new Commands.PlayAttackCardCommand.Types.Response.Types.InvalidAccessToken(),
            };
        }

        var game = gameCollection.Get(gameAccessToken.GameId);
        game.Execute(new Domain.GameCommands.PlayAttackCard(playerIdentity.PlayerId, request.CardId));

        return new Commands.PlayAttackCardCommand.Types.Response()
        {
            Success = new Commands.PlayAttackCardCommand.Types.Response.Types.Success(),
        };
    }
}
