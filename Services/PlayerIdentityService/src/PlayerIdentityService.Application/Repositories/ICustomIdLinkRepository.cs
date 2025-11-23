namespace PlayerIdentityService.Application;

public interface ICustomIdLinkRepository
{
    public Task Create(string customId);
    public Task<Domain.CustomIdLinkState?> TryGet(string customId);
    public Task ApplyDomainEvent(string customId, object @event);
}

