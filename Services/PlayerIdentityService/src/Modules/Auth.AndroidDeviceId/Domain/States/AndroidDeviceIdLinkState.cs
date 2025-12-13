namespace PlayerIdentityService.Modules.Auth.AndroidDeviceId.Domain;

public record AndroidDeviceIdLinkState : Eventuous.State<AndroidDeviceIdLinkState>
{
    public string PlayerId { get; init; } = string.Empty;

    public AndroidDeviceIdLinkState()
    {
        On<AndroidDeviceIdLinkEvents.LinkCreated>(static (state, @event) => state with { PlayerId = @event.PlayerId });
    }
}
