using PlayerIdentityService.Modules.Auth.CustomId.Domain;

namespace PlayerIdentityService.Modules.Auth.CustomId.Application.Repositories;

public interface ICustomIdLinkRepository
{
    public Task Create(string customId);
    public Task<CustomIdLinkState?> TryGet(string customId);
    public Task ApplyDomainEvent(string customId, object @event);
}
