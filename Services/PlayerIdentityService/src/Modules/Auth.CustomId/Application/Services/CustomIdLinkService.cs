using PlayerIdentityService.Modules.Auth.CustomId.Application.Repositories;
using PlayerIdentityService.Modules.Auth.CustomId.Domain;

namespace PlayerIdentityService.Modules.Auth.CustomId.Application.Services;

public class CustomIdLinkService(ICustomIdLinkRepository customIdLinkRepository)
{
    public Task<CustomIdLinkState?> TryGet(string customId)
    {
        return customIdLinkRepository.TryGet(customId);
    }

    public async Task<CustomIdLinkState> Create(string customId, string playerId)
    {
        var customIdLink = new CustomIdLinkState();

        var linkCreatedEvent = new CustomIdLinkEvents.LinkCreated(playerId);
        customIdLink = customIdLink.When(linkCreatedEvent);

        await customIdLinkRepository.Create(customId);
        await customIdLinkRepository.ApplyDomainEvent(customId, linkCreatedEvent);

        return customIdLink;
    }
}
