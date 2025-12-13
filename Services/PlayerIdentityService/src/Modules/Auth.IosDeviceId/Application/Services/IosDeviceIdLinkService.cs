using PlayerIdentityService.Modules.Auth.IosDeviceId.Application.Repositories;
using PlayerIdentityService.Modules.Auth.IosDeviceId.Domain;

namespace PlayerIdentityService.Modules.Auth.IosDeviceId.Application.Services;

public class IosDeviceIdLinkService(IIosDeviceIdLinkRepository iosDeviceIdLinkRepository)
{
    public Task<IosDeviceIdLinkState?> TryGet(string deviceId)
    {
        return iosDeviceIdLinkRepository.TryGet(deviceId);
    }

    public async Task<IosDeviceIdLinkState> Create(string deviceId, string playerId)
    {
        var link = new IosDeviceIdLinkState();

        var linkCreatedEvent = new IosDeviceIdLinkEvents.LinkCreated(playerId);
        link = link.When(linkCreatedEvent);

        await iosDeviceIdLinkRepository.Create(deviceId);
        await iosDeviceIdLinkRepository.ApplyDomainEvent(deviceId, linkCreatedEvent);

        return link;
    }
}
