using MediatR;
using PlayerIdentityService.Modules.Players.Application.Services;
using PlayerIdentityService.Modules.Players.Contracts;

namespace PlayerIdentityService.Modules.Players.Application.Handlers;

public class CreatePlayerCommandHandler(PlayerService playerService) : IRequestHandler<CreatePlayerCommand, string>
{
    public Task<string> Handle(CreatePlayerCommand request, CancellationToken cancellationToken)
    {
        return playerService.Create();
    }
}
