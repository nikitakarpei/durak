namespace PlayerIdentityService.Application;

public interface IAndroidDeviceIdLinkRepository
{
    public Task Create(string deviceId);
    public Task<Domain.AndroidDeviceIdLinkState?> TryGet(string deviceId);
    public Task ApplyDomainEvent(string deviceId, object @event);
}
