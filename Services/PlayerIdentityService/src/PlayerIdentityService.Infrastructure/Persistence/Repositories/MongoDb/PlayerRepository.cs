using MongoDB.Driver;
using PlayerIdentityService.Infrastructure.Persistence.Database.MongoDb;
using PlayerIdentityService.Infrastructure.Persistence.Models.MongoDb;

namespace PlayerIdentityService.Infrastructure.Persistence.Repositories.MongoDb;

public class PlayerRepository(IMongoSessionProvider sessionProvider, IMongoCollection<PlayerModel> players)
    : Application.IPlayerRepository
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
