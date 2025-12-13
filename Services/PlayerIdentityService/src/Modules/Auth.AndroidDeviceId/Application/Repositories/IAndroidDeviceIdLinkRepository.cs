using PlayerIdentityService.Modules.Auth.AndroidDeviceId.Domain;

namespace PlayerIdentityService.Modules.Auth.AndroidDeviceId.Application.Repositories;

public interface IAndroidDeviceIdLinkRepository
{
    public Task Create(string deviceId);
    public Task<AndroidDeviceIdLinkState?> TryGet(string deviceId);
    public Task ApplyDomainEvent(string deviceId, object @event);
}
