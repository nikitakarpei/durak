using PlayerIdentityService.Modules.Players.Application.Authentication;

namespace PlayerIdentityService.Modules.Players.Api.CommandHandlers;

public class GetIdentityTokenPublicKeyCommandHandler(IPlayerIdentityTokenIssuer tokenIssuer)
    : Rpc.Server.IUnaryCallHandler<
        PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request,
        PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response
    >
{
    public async Task<PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response> HandleAsync(
        PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        return new PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response()
        {
            Key = await tokenIssuer.GetPublicKeyAsync(cancellationToken),
        };
    }
}
