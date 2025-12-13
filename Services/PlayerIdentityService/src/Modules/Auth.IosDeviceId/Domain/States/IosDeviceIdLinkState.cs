namespace PlayerIdentityService.Modules.Auth.IosDeviceId.Domain;

public record IosDeviceIdLinkState : Eventuous.State<IosDeviceIdLinkState>
{
    public string PlayerId { get; init; } = string.Empty;

    public IosDeviceIdLinkState()
    {
        On<IosDeviceIdLinkEvents.LinkCreated>(static (state, @event) => state with { PlayerId = @event.PlayerId });
    }
}
