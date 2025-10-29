using System.Collections.Concurrent;

namespace PlayerIdentityService.Infrastructure.Persistence.Repositories.Memory;

public class PlayerRepository : Application.IPlayerRepository
{
    private readonly ConcurrentDictionary<string, bool> _players = new();

    public Task Create(string playerId)
    {
        _players.TryAdd(playerId, true);
        return Task.CompletedTask;
    }

    public Task<bool> Exists(string playerId)
    {
        return Task.FromResult(_players.ContainsKey(playerId));
    }
}
