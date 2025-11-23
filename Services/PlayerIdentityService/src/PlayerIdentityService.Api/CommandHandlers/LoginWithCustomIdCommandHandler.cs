namespace PlayerIdentityService.Api;

public class LoginWithCustomIdCommandHandler(
    Application.PlayerService playerService,
    Application.IPlayerIdentityTokenIssuer playerIdentityTokenIssuer,
    Application.CustomIdLinkService customIdLinkService
)
    : Rpc.Server.IUnaryCallHandler<
        Commands.LoginWithCustomIdCommand.Types.Request,
        Commands.LoginWithCustomIdCommand.Types.Response
    >
{
    public async Task<Commands.LoginWithCustomIdCommand.Types.Response> HandleAsync(
        Commands.LoginWithCustomIdCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        bool newlyCreated = false;

        var customIdLink = await customIdLinkService.TryGet(request.CustomId);
        if (customIdLink is null)
        {
            var playerId = await playerService.Create();
            newlyCreated = true;

            customIdLink = await customIdLinkService.Create(request.CustomId, playerId);
        }

        var playerIdentityToken = await playerIdentityTokenIssuer.IssueTokenAsync(
            customIdLink.PlayerId,
            cancellationToken
        );

        return new Commands.LoginWithCustomIdCommand.Types.Response()
        {
            Result = new Data.LoginResult()
            {
                PlayerId = customIdLink.PlayerId,
                PlayerIdToken = playerIdentityToken,
                NewlyCreated = newlyCreated,
            },
        };
    }
}
