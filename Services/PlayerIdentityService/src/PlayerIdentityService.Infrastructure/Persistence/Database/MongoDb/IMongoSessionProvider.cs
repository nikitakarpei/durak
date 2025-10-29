using MongoDB.Driver;

namespace PlayerIdentityService.Infrastructure.Persistence.Database.MongoDb;

public interface IMongoSessionProvider
{
    IClientSessionHandle? Current { get; }
}
