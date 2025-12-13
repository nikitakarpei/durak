namespace PlayerIdentityService.Modules.Auth.AndroidDeviceId.Domain;

public static class AndroidDeviceIdLinkEvents
{
    public record LinkCreated(string PlayerId);
}
