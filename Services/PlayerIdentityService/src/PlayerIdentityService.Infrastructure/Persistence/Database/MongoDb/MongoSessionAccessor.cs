using MongoDB.Driver;

namespace PlayerIdentityService.Infrastructure.Persistence.Database.MongoDb;

public class MongoSessionAccessor : IMongoSessionProvider
{
    private IClientSessionHandle? _current;
    public IClientSessionHandle? Current
    {
        get => _current;
        set => _current = value;
    }
}
