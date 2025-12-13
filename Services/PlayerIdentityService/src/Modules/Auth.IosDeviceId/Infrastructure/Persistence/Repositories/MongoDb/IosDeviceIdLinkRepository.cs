using MongoDB.Driver;
using PlayerIdentityService.BuildingBlocks.Infrastructure;
using PlayerIdentityService.Modules.Auth.IosDeviceId.Application.Repositories;
using PlayerIdentityService.Modules.Auth.IosDeviceId.Domain;
using PlayerIdentityService.Modules.Auth.IosDeviceId.Infrastructure.Persistence.Models.MongoDb;

namespace PlayerIdentityService.Modules.Auth.IosDeviceId.Infrastructure.Persistence.Repositories.MongoDb;

public class IosDeviceIdLinkRepository(
    IMongoSessionProvider sessionProvider,
    IMongoCollection<IosDeviceIdLinkModel> iosDeviceIdLinks
) : IIosDeviceIdLinkRepository
{
    public Task Create(string deviceId)
    {
        return iosDeviceIdLinks.InsertOneAsync(
            sessionProvider.Current,
            new IosDeviceIdLinkModel(deviceId, string.Empty)
        );
    }

    public async Task<IosDeviceIdLinkState?> TryGet(string deviceId)
    {
        var model = await iosDeviceIdLinks
            .Find(sessionProvider.Current, link => link.Id == deviceId)
            .FirstOrDefaultAsync();
        if (model is null)
        {
            return null;
        }

        return new IosDeviceIdLinkState() with
        {
            PlayerId = model.PlayerId,
        };
    }

    public Task ApplyDomainEvent(string deviceId, object @event)
    {
        return @event switch
        {
            IosDeviceIdLinkEvents.LinkCreated linkCreatedEvent => iosDeviceIdLinks.UpdateOneAsync(
                sessionProvider.Current,
                link => link.Id == deviceId,
                Builders<IosDeviceIdLinkModel>.Update.Set(link => link.PlayerId, linkCreatedEvent.PlayerId)
            ),
            _ => throw new ArgumentException($"Unknown event type: {@event.GetType().Name}"),
        };
    }
}
