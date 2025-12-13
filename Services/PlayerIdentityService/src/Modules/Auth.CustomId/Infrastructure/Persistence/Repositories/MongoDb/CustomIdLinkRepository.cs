using MongoDB.Driver;
using PlayerIdentityService.BuildingBlocks.Infrastructure;
using PlayerIdentityService.Modules.Auth.CustomId.Application.Repositories;
using PlayerIdentityService.Modules.Auth.CustomId.Domain;
using PlayerIdentityService.Modules.Auth.CustomId.Infrastructure.Persistence.Models.MongoDb;

namespace PlayerIdentityService.Modules.Auth.CustomId.Infrastructure.Persistence.Repositories.MongoDb;

public class CustomIdLinkRepository(
    IMongoSessionProvider sessionProvider,
    IMongoCollection<CustomIdLinkModel> customIdLinks
) : ICustomIdLinkRepository
{
    public Task Create(string customId)
    {
        return customIdLinks.InsertOneAsync(sessionProvider.Current, new CustomIdLinkModel(customId, string.Empty));
    }

    public async Task<CustomIdLinkState?> TryGet(string customId)
    {
        var model = await customIdLinks
            .Find(sessionProvider.Current, link => link.Id == customId)
            .FirstOrDefaultAsync();
        if (model is null)
        {
            return null;
        }

        return new CustomIdLinkState() with
        {
            PlayerId = model.PlayerId,
        };
    }

    public Task ApplyDomainEvent(string customId, object @event)
    {
        return @event switch
        {
            CustomIdLinkEvents.LinkCreated linkCreatedEvent => customIdLinks.UpdateOneAsync(
                sessionProvider.Current,
                link => link.Id == customId,
                Builders<CustomIdLinkModel>.Update.Set(link => link.PlayerId, linkCreatedEvent.PlayerId)
            ),
            _ => throw new ArgumentException($"Unknown event type: {@event.GetType().Name}"),
        };
    }
}
