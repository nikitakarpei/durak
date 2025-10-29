namespace PlayerIdentityService.Domain;

public record PlayerState : Eventuous.State<PlayerState>
{
    public PlayerState()
    {
        On<PlayerEvents.PlayerCreated>(static (state, @event) => state with { });
    }
}
