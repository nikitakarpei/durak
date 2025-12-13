namespace PlayerIdentityService.Modules.Auth.CustomId.Domain;

public record CustomIdLinkState : Eventuous.State<CustomIdLinkState>
{
    public string PlayerId { get; init; } = string.Empty;

    public CustomIdLinkState()
    {
        On<CustomIdLinkEvents.LinkCreated>(static (state, @event) => state with { PlayerId = @event.PlayerId });
    }
}
