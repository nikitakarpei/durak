using MediatR;

namespace PlayerIdentityService.Modules.Players.Contracts;

public record IssueIdentityTokenCommand(string PlayerId) : IRequest<string>;
