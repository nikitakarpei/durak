namespace PlayerIdentityService.Domain;

public record IosDeviceIdLinkState : Eventuous.State<IosDeviceIdLinkState>
{
    public IosDeviceIdLinkState()
    {
        On<IosDeviceIdLinkEvents.LinkCreated>(static (state, @event) => state with { PlayerId = @event.PlayerId });
    }

    public string PlayerId { get; init; } = string.Empty;
}
