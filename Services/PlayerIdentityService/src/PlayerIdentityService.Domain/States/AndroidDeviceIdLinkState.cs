namespace PlayerIdentityService.Domain;

public record AndroidDeviceIdLinkState : Eventuous.State<AndroidDeviceIdLinkState>
{
    public AndroidDeviceIdLinkState()
    {
        On<AndroidDeviceIdLinkEvents.LinkCreated>(static (state, @event) => state with { PlayerId = @event.PlayerId });
    }

    public string PlayerId { get; init; } = string.Empty;
}
