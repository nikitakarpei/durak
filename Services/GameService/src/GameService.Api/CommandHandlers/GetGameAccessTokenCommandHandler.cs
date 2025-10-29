namespace GameService.Api;

public class GetGameAccessTokenCommandHandler(Application.IGameAccessTokenIssuer gameAccessTokenIssuer)
    : Rpc.Server.IUnaryCallHandler<
        Commands.GetGameAccessTokenCommand.Types.Request,
        Commands.GetGameAccessTokenCommand.Types.Response
    >
{
    public async Task<Commands.GetGameAccessTokenCommand.Types.Response> HandleAsync(
        Commands.GetGameAccessTokenCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        // todo: check if player has access to the game

        var gameAccessToken = await gameAccessTokenIssuer.IssueAsync(request.GameId, cancellationToken);

        return new Commands.GetGameAccessTokenCommand.Types.Response()
        {
            Success = new Commands.GetGameAccessTokenCommand.Types.Response.Types.Success()
            {
                AccessToken = gameAccessToken,
            },
        };
    }
}
