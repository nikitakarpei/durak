using MediatR;
using PlayerIdentityService.Modules.Players.Application.Authentication;
using PlayerIdentityService.Modules.Players.Contracts;

namespace PlayerIdentityService.Modules.Players.Application.Handlers;

public class IssueIdentityTokenCommandHandler(IPlayerIdentityTokenIssuer tokenIssuer)
    : IRequestHandler<IssueIdentityTokenCommand, string>
{
    public Task<string> Handle(IssueIdentityTokenCommand request, CancellationToken cancellationToken)
    {
        return tokenIssuer.IssueTokenAsync(request.PlayerId, cancellationToken);
    }
}
