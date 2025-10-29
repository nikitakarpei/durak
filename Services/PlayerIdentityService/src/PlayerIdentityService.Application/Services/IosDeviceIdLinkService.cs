namespace PlayerIdentityService.Application;

public class IosDeviceIdLinkService(IIosDeviceIdLinkRepository iosDeviceIdLinkRepository)
{
    public async Task<Domain.IosDeviceIdLinkState?> TryGet(string deviceId)
    {
        return await iosDeviceIdLinkRepository.TryGet(deviceId);
    }

    public async Task<Domain.IosDeviceIdLinkState> Create(string deviceId, string playerId)
    {
        var iosDeviceIdLink = new Domain.IosDeviceIdLinkState();

        var linkCreatedEvent = new Domain.IosDeviceIdLinkEvents.LinkCreated(playerId);
        iosDeviceIdLink = iosDeviceIdLink.When(linkCreatedEvent);

        await iosDeviceIdLinkRepository.Create(deviceId);
        await iosDeviceIdLinkRepository.ApplyDomainEvent(deviceId, linkCreatedEvent);

        return iosDeviceIdLink;
    }
}
