namespace PlayerIdentityService.Application;

public interface IIosDeviceIdLinkRepository
{
    public Task Create(string deviceId);
    public Task<Domain.IosDeviceIdLinkState?> TryGet(string deviceId);
    public Task ApplyDomainEvent(string deviceId, object @event);
}
