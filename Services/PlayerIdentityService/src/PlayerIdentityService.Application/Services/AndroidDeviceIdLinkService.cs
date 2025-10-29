namespace PlayerIdentityService.Application;

public class AndroidDeviceIdLinkService(IAndroidDeviceIdLinkRepository androidDeviceIdLinkRepository)
{
    public async Task<Domain.AndroidDeviceIdLinkState?> TryGet(string deviceId)
    {
        return await androidDeviceIdLinkRepository.TryGet(deviceId);
    }

    public async Task<Domain.AndroidDeviceIdLinkState> Create(string deviceId, string playerId)
    {
        var androidDeviceIdLink = new Domain.AndroidDeviceIdLinkState();

        var linkCreatedEvent = new Domain.AndroidDeviceIdLinkEvents.LinkCreated(playerId);
        androidDeviceIdLink = androidDeviceIdLink.When(linkCreatedEvent);

        await androidDeviceIdLinkRepository.Create(deviceId);
        await androidDeviceIdLinkRepository.ApplyDomainEvent(deviceId, linkCreatedEvent);

        return androidDeviceIdLink;
    }
}
