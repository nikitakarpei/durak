using MediatR;
using PlayerIdentityService.Modules.Auth.AndroidDeviceId.Application.Services;
using PlayerIdentityService.Modules.Players.Contracts;

namespace PlayerIdentityService.Modules.Auth.AndroidDeviceId.Api.CommandHandlers;

public class LoginWithAndroidDeviceIdCommandHandler(
    ISender sender,
    AndroidDeviceIdLinkService androidDeviceIdLinkService
)
    : Rpc.Server.IUnaryCallHandler<
        PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request,
        PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Response
    >
{
    public async Task<PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Response> HandleAsync(
        PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        bool newlyCreated = false;

        var link = await androidDeviceIdLinkService.TryGet(request.DeviceId);
        if (link is null)
        {
            var playerId = await sender.Send(new CreatePlayerCommand(), cancellationToken);
            newlyCreated = true;

            link = await androidDeviceIdLinkService.Create(request.DeviceId, playerId);
        }

        var playerIdentityToken = await sender.Send(new IssueIdentityTokenCommand(link.PlayerId), cancellationToken);

        return new PlayerIdentityService.Api.Commands.LoginWithAndroidDeviceIdCommand.Types.Response()
        {
            Result = new PlayerIdentityService.Api.Data.LoginResult()
            {
                PlayerId = link.PlayerId,
                PlayerIdToken = playerIdentityToken,
                NewlyCreated = newlyCreated,
            },
        };
    }
}
