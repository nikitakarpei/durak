using PlayerIdentityService.Modules.Players.Application.Repositories;

namespace PlayerIdentityService.Modules.Players.Application.Services;

public class PlayerService(IPlayerRepository playerRepository)
{
    public async Task<string> Create()
    {
        var playerId = Guid.NewGuid().ToString();
        await playerRepository.Create(playerId);
        return playerId;
    }
}
