using MongoDB.Driver;
using PlayerIdentityService.BuildingBlocks.Infrastructure;
using PlayerIdentityService.Modules.Auth.AndroidDeviceId.Application.Repositories;
using PlayerIdentityService.Modules.Auth.AndroidDeviceId.Domain;
using PlayerIdentityService.Modules.Auth.AndroidDeviceId.Infrastructure.Persistence.Models.MongoDb;

namespace PlayerIdentityService.Modules.Auth.AndroidDeviceId.Infrastructure.Persistence.Repositories.MongoDb;

public class AndroidDeviceIdLinkRepository(
    IMongoSessionProvider sessionProvider,
    IMongoCollection<AndroidDeviceIdLinkModel> androidDeviceIdLinks
) : IAndroidDeviceIdLinkRepository
{
    public Task Create(string deviceId)
    {
        return androidDeviceIdLinks.InsertOneAsync(
            sessionProvider.Current,
            new AndroidDeviceIdLinkModel(deviceId, string.Empty)
        );
    }

    public async Task<AndroidDeviceIdLinkState?> TryGet(string deviceId)
    {
        var model = await androidDeviceIdLinks
            .Find(sessionProvider.Current, link => link.Id == deviceId)
            .FirstOrDefaultAsync();
        if (model is null)
        {
            return null;
        }

        return new AndroidDeviceIdLinkState() with
        {
            PlayerId = model.PlayerId,
        };
    }

    public Task ApplyDomainEvent(string deviceId, object @event)
    {
        return @event switch
        {
            AndroidDeviceIdLinkEvents.LinkCreated linkCreatedEvent => androidDeviceIdLinks.UpdateOneAsync(
                sessionProvider.Current,
                link => link.Id == deviceId,
                Builders<AndroidDeviceIdLinkModel>.Update.Set(link => link.PlayerId, linkCreatedEvent.PlayerId)
            ),
            _ => throw new ArgumentException($"Unknown event type: {@event.GetType().Name}"),
        };
    }
}
