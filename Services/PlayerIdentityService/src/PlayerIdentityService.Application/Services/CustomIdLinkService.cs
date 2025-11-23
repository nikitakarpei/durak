namespace PlayerIdentityService.Application;

public class CustomIdLinkService(ICustomIdLinkRepository customIdLinkRepository)
{
    public async Task<Domain.CustomIdLinkState?> TryGet(string customId)
    {
        return await customIdLinkRepository.TryGet(customId);
    }

    public async Task<Domain.CustomIdLinkState> Create(string customId, string playerId)
    {
        var customIdLink = new Domain.CustomIdLinkState();

        var linkCreatedEvent = new Domain.CustomIdLinkEvents.LinkCreated(playerId);
        customIdLink = customIdLink.When(linkCreatedEvent);

        await customIdLinkRepository.Create(customId);
        await customIdLinkRepository.ApplyDomainEvent(customId, linkCreatedEvent);

        return customIdLink;
    }
}

