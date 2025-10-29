namespace GameService.Api;

public class BeatAttackCardCommandHandler(
    Application.IGameCollection gameCollection,
    Application.IGameAccessTokenVerifier gameAccessTokenVerifier,
    Application.IVerifiedPlayerIdentityProvider verifiedPlayerIdentityProvider
)
    : Rpc.Server.IUnaryCallHandler<
        Commands.BeatAttackCardCommand.Types.Request,
        Commands.BeatAttackCardCommand.Types.Response
    >
{
    public async Task<Commands.BeatAttackCardCommand.Types.Response> HandleAsync(
        Commands.BeatAttackCardCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        var playerIdentity =
            verifiedPlayerIdentityProvider.Identity ?? throw new InvalidOperationException("Player identity not found");

        var gameAccessToken = await gameAccessTokenVerifier.VerifyAsync(request.AccessToken, cancellationToken);
        if (gameAccessToken is null)
        {
            return new Commands.BeatAttackCardCommand.Types.Response()
            {
                InvalidAccessToken = new Commands.BeatAttackCardCommand.Types.Response.Types.InvalidAccessToken(),
            };
        }

        var game = gameCollection.Get(gameAccessToken.GameId);
        game.When(
            new Domain.GameCommands.BeatAttackCard(
                playerIdentity.PlayerId,
                request.AttackerCardId,
                request.DefenderCardId
            )
        );

        return new Commands.BeatAttackCardCommand.Types.Response()
        {
            Success = new Commands.BeatAttackCardCommand.Types.Response.Types.Success(),
        };
    }
}
