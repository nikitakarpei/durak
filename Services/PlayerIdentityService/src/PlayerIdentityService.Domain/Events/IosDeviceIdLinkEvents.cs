namespace PlayerIdentityService.Domain;

public static class IosDeviceIdLinkEvents
{
    public record LinkCreated(string PlayerId);
}
