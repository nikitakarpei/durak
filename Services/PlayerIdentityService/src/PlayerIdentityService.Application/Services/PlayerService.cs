namespace PlayerIdentityService.Application;

public class PlayerService(IPlayerRepository playerRepository)
{
    public async Task<string> Create()
    {
        var playerId = Guid.NewGuid().ToString();
        await playerRepository.Create(playerId);
        return playerId;
    }
}
