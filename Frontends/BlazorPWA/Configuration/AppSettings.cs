namespace BlazorPWA.Configuration;

public sealed class AppSettings
{
    public ServicesOptions Services { get; init; } = new();
    public AuthOptions Auth { get; init; } = new();

    public sealed class ServicesOptions
    {
        public string GameServiceUrl { get; init; } = string.Empty;
        public string PlayerIdentityServiceUrl { get; init; } = string.Empty;
    }

    public sealed class AuthOptions
    {
        public string CustomId { get; init; } = string.Empty;
    }
}
