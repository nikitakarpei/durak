namespace PlayerIdentityService.Api;

public class LoginWithIosDeviceIdCommandHandler(
    Application.PlayerService playerService,
    Application.IPlayerIdentityTokenIssuer playerIdentityTokenIssuer,
    Application.IosDeviceIdLinkService iosDeviceIdLinkService
)
    : Rpc.Server.IUnaryCallHandler<
        Commands.LoginWithIosDeviceIdCommand.Types.Request,
        Commands.LoginWithIosDeviceIdCommand.Types.Response
    >
{
    public async Task<Commands.LoginWithIosDeviceIdCommand.Types.Response> HandleAsync(
        Commands.LoginWithIosDeviceIdCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        bool newlyCreated = false;

        var iosDeviceIdLink = await iosDeviceIdLinkService.TryGet(request.DeviceId);
        if (iosDeviceIdLink is null)
        {
            var playerId = await playerService.Create();
            newlyCreated = true;

            iosDeviceIdLink = await iosDeviceIdLinkService.Create(request.DeviceId, playerId);
        }

        var playerIdentityToken = await playerIdentityTokenIssuer.IssueTokenAsync(
            iosDeviceIdLink.PlayerId,
            cancellationToken
        );

        return new Commands.LoginWithIosDeviceIdCommand.Types.Response()
        {
            Result = new Data.LoginResult()
            {
                PlayerId = iosDeviceIdLink.PlayerId,
                PlayerIdToken = playerIdentityToken,
                NewlyCreated = newlyCreated,
            },
        };
    }
}
