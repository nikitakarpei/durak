namespace PlayerIdentityService.Api;

public class GetIdentityTokenPublicKeyCommandHandler(Application.IPlayerIdentityTokenIssuer playerIdentityTokenIssuer)
    : Rpc.Server.IUnaryCallHandler<
        Commands.GetIdentityTokenPublicKeyCommand.Types.Request,
        Commands.GetIdentityTokenPublicKeyCommand.Types.Response
    >
{
    public async Task<Commands.GetIdentityTokenPublicKeyCommand.Types.Response> HandleAsync(
        Commands.GetIdentityTokenPublicKeyCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        return new Commands.GetIdentityTokenPublicKeyCommand.Types.Response()
        {
            Key = await playerIdentityTokenIssuer.GetPublicKeyAsync(cancellationToken),
        };
    }
}
