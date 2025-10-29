namespace PlayerIdentityService.Domain;

public static class AndroidDeviceIdLinkEvents
{
    public record LinkCreated(string PlayerId);
}
