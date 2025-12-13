namespace PlayerIdentityService.Modules.Players.Application.Repositories;

public interface IPlayerRepository
{
    public Task Create(string playerId);
    public Task<bool> Exists(string playerId);
}
