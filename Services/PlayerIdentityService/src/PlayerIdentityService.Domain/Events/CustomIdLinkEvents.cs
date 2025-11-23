namespace PlayerIdentityService.Domain;

public static class CustomIdLinkEvents
{
    public record LinkCreated(string PlayerId);
}

