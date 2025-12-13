using MongoDB.Driver;
using PlayerIdentityService.BuildingBlocks.Infrastructure;
using PlayerIdentityService.Modules.Players.Application.Repositories;
using PlayerIdentityService.Modules.Players.Infrastructure.Persistence.Models.MongoDb;

namespace PlayerIdentityService.Modules.Players.Infrastructure.Persistence.Repositories.MongoDb;

public class PlayerRepository(IMongoSessionProvider sessionProvider, IMongoCollection<PlayerModel> players)
    : IPlayerRepository
{
    public async Task Create(string playerId)
    {
        await players.InsertOneAsync(sessionProvider.Current, new PlayerModel(playerId));
    }

    public async Task<bool> Exists(string playerId)
    {
        return await players.Find(sessionProvider.Current, p => p.Id == playerId).AnyAsync();
    }
}
