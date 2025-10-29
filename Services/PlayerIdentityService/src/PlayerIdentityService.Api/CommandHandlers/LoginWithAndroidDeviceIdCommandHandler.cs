namespace PlayerIdentityService.Api;

public class LoginWithAndroidDeviceIdCommandHandler(
    Application.PlayerService playerService,
    Application.IPlayerIdentityTokenIssuer playerIdentityTokenIssuer,
    Application.AndroidDeviceIdLinkService androidDeviceIdLinkService
)
    : Rpc.Server.IUnaryCallHandler<
        Commands.LoginWithAndroidDeviceIdCommand.Types.Request,
        Commands.LoginWithAndroidDeviceIdCommand.Types.Response
    >
{
    public async Task<Commands.LoginWithAndroidDeviceIdCommand.Types.Response> HandleAsync(
        Commands.LoginWithAndroidDeviceIdCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        bool newlyCreated = false;

        var androidDeviceIdLink = await androidDeviceIdLinkService.TryGet(request.DeviceId);
        if (androidDeviceIdLink is null)
        {
            var playerId = await playerService.Create();
            newlyCreated = true;

            androidDeviceIdLink = await androidDeviceIdLinkService.Create(request.DeviceId, playerId);
        }

        var playerIdentityToken = await playerIdentityTokenIssuer.IssueTokenAsync(
            androidDeviceIdLink.PlayerId,
            cancellationToken
        );

        return new Commands.LoginWithAndroidDeviceIdCommand.Types.Response()
        {
            Result = new Data.LoginResult()
            {
                PlayerId = androidDeviceIdLink.PlayerId,
                PlayerIdToken = playerIdentityToken,
                NewlyCreated = newlyCreated,
            },
        };
    }
}
