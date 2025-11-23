namespace PlayerIdentityService.Domain;

public record CustomIdLinkState : Eventuous.State<CustomIdLinkState>
{
    public CustomIdLinkState()
    {
        On<CustomIdLinkEvents.LinkCreated>(static (state, @event) => state with { PlayerId = @event.PlayerId });
    }

    public string PlayerId { get; init; } = string.Empty;
}

