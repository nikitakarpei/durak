using PlayerIdentityService.Modules.Auth.AndroidDeviceId.Application.Repositories;
using PlayerIdentityService.Modules.Auth.AndroidDeviceId.Domain;

namespace PlayerIdentityService.Modules.Auth.AndroidDeviceId.Application.Services;

public class AndroidDeviceIdLinkService(IAndroidDeviceIdLinkRepository androidDeviceIdLinkRepository)
{
    public Task<AndroidDeviceIdLinkState?> TryGet(string deviceId)
    {
        return androidDeviceIdLinkRepository.TryGet(deviceId);
    }

    public async Task<AndroidDeviceIdLinkState> Create(string deviceId, string playerId)
    {
        var link = new AndroidDeviceIdLinkState();

        var linkCreatedEvent = new AndroidDeviceIdLinkEvents.LinkCreated(playerId);
        link = link.When(linkCreatedEvent);

        await androidDeviceIdLinkRepository.Create(deviceId);
        await androidDeviceIdLinkRepository.ApplyDomainEvent(deviceId, linkCreatedEvent);

        return link;
    }
}
