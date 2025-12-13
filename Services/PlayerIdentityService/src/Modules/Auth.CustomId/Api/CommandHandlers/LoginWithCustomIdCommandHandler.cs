using MediatR;
using PlayerIdentityService.Modules.Auth.CustomId.Application.Services;
using PlayerIdentityService.Modules.Players.Contracts;

namespace PlayerIdentityService.Modules.Auth.CustomId.Api.CommandHandlers;

public class LoginWithCustomIdCommandHandler(ISender sender, CustomIdLinkService customIdLinkService)
    : Rpc.Server.IUnaryCallHandler<
        PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request,
        PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Response
    >
{
    public async Task<PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Response> HandleAsync(
        PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Request request,
        CancellationToken cancellationToken
    )
    {
        bool newlyCreated = false;

        var customIdLink = await customIdLinkService.TryGet(request.CustomId);
        if (customIdLink is null)
        {
            var playerId = await sender.Send(new CreatePlayerCommand(), cancellationToken);
            newlyCreated = true;

            customIdLink = await customIdLinkService.Create(request.CustomId, playerId);
        }

        var playerIdentityToken = await sender.Send(
            new IssueIdentityTokenCommand(customIdLink.PlayerId),
            cancellationToken
        );

        return new PlayerIdentityService.Api.Commands.LoginWithCustomIdCommand.Types.Response()
        {
            Result = new PlayerIdentityService.Api.Data.LoginResult()
            {
                PlayerId = customIdLink.PlayerId,
                PlayerIdToken = playerIdentityToken,
                NewlyCreated = newlyCreated,
            },
        };
    }
}
