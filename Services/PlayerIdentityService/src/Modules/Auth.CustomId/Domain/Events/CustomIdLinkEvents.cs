namespace PlayerIdentityService.Modules.Auth.CustomId.Domain;

public static class CustomIdLinkEvents
{
    public record LinkCreated(string PlayerId);
}
