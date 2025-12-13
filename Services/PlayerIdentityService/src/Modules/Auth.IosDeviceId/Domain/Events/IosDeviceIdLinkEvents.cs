namespace PlayerIdentityService.Modules.Auth.IosDeviceId.Domain;

public static class IosDeviceIdLinkEvents
{
    public record LinkCreated(string PlayerId);
}
