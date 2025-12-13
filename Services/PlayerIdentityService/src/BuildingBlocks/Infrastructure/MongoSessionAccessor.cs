using MongoDB.Driver;

namespace PlayerIdentityService.BuildingBlocks.Infrastructure;

public class MongoSessionAccessor : IMongoSessionProvider
{
    private IClientSessionHandle? _current;
    public IClientSessionHandle? Current
    {
        get => _current;
        set => _current = value;
    }
}
