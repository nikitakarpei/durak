using MongoDB.Driver;

namespace PlayerIdentityService.BuildingBlocks.Infrastructure;

public interface IMongoSessionProvider
{
    IClientSessionHandle? Current { get; }
}
