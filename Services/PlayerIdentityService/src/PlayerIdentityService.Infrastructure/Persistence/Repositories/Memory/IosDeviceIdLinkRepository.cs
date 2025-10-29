using System.Collections.Concurrent;
using PlayerIdentityService.Domain;

namespace PlayerIdentityService.Infrastructure.Persistence.Repositories.Memory;

public class IosDeviceIdLinkRepository : Application.IIosDeviceIdLinkRepository
{
    private readonly ConcurrentDictionary<string, IosDeviceIdLinkState> _links = new();

    public Task Create(string deviceId)
    {
        _links.TryAdd(deviceId, new IosDeviceIdLinkState());
        return Task.CompletedTask;
    }

    public Task<IosDeviceIdLinkState?> TryGet(string deviceId)
    {
        _links.TryGetValue(deviceId, out var state);
        return Task.FromResult(state);
    }

    public Task ApplyDomainEvent(string deviceId, object @event)
    {
        return @event switch
        {
            IosDeviceIdLinkEvents.LinkCreated linkCreatedEvent => UpdateState(
                deviceId,
                state => state with { PlayerId = linkCreatedEvent.PlayerId }
            ),
            _ => throw new ArgumentException($"Unknown event type: {@event.GetType().Name}"),
        };
    }

    private Task UpdateState(string deviceId, Func<IosDeviceIdLinkState, IosDeviceIdLinkState> update)
    {
        _links.AddOrUpdate(deviceId, _ => update(new IosDeviceIdLinkState()), (_, state) => update(state));
        return Task.CompletedTask;
    }
}
