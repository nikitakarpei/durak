using MediatR;
using PlayerIdentityService.Modules.Auth.IosDeviceId.Application.Services;
using PlayerIdentityService.Modules.Players.Contracts;

namespace PlayerIdentityService.Modules.Auth.IosDeviceId.Api.CommandHandlers;

public class LoginWithIosDeviceIdCommandHandler(ISender sender, IosDeviceIdLinkService iosDeviceIdLinkService)
    : Rpc.Server.IUnaryCallHandler<
        PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Request,
        PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Response
    >
{
    public async Task<PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Response> HandleAsync(
        PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        bool newlyCreated = false;

        var link = await iosDeviceIdLinkService.TryGet(request.DeviceId);
        if (link is null)
        {
            var playerId = await sender.Send(new CreatePlayerCommand(), cancellationToken);
            newlyCreated = true;

            link = await iosDeviceIdLinkService.Create(request.DeviceId, playerId);
        }

        var playerIdentityToken = await sender.Send(new IssueIdentityTokenCommand(link.PlayerId), cancellationToken);

        return new PlayerIdentityService.Api.Commands.LoginWithIosDeviceIdCommand.Types.Response()
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
