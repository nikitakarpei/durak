using PlayerIdentityService.Modules.Auth.IosDeviceId.Domain;

namespace PlayerIdentityService.Modules.Auth.IosDeviceId.Application.Repositories;

public interface IIosDeviceIdLinkRepository
{
    public Task Create(string deviceId);
    public Task<IosDeviceIdLinkState?> TryGet(string deviceId);
    public Task ApplyDomainEvent(string deviceId, object @event);
}
