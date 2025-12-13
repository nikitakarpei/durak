using MediatR;

namespace PlayerIdentityService.Modules.Players.Contracts;

public record CreatePlayerCommand() : IRequest<string>;
