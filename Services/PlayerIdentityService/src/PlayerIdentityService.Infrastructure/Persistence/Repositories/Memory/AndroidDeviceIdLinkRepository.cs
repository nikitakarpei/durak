using System.Collections.Concurrent;
using PlayerIdentityService.Domain;

namespace PlayerIdentityService.Infrastructure.Persistence.Repositories.Memory;

public class AndroidDeviceIdLinkRepository : Application.IAndroidDeviceIdLinkRepository
{
    private readonly ConcurrentDictionary<string, AndroidDeviceIdLinkState> _links = new();

    public Task Create(string deviceId)
    {
        _links.TryAdd(deviceId, new AndroidDeviceIdLinkState());
        return Task.CompletedTask;
    }

    public Task<AndroidDeviceIdLinkState?> TryGet(string deviceId)
    {
        _links.TryGetValue(deviceId, out var state);
        return Task.FromResult(state);
    }

    public Task ApplyDomainEvent(string deviceId, object @event)
    {
        return @event switch
        {
            AndroidDeviceIdLinkEvents.LinkCreated linkCreatedEvent => UpdateState(
                deviceId,
                state => state with { PlayerId = linkCreatedEvent.PlayerId }
            ),
            _ => throw new ArgumentException($"Unknown event type: {@event.GetType().Name}"),
        };
    }

    private Task UpdateState(string deviceId, Func<AndroidDeviceIdLinkState, AndroidDeviceIdLinkState> update)
    {
        _links.AddOrUpdate(deviceId, _ => update(new AndroidDeviceIdLinkState()), (_, state) => update(state));
        return Task.CompletedTask;
    }
}
