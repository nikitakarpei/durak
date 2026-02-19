namespace BlazorPWA.Services.Api;

public interface IPlayerIdentityServiceClient
{
    Task<PlayerIdentityService.Api.Data.LoginResult> LoginWithCustomIdAsync(string customId, CancellationToken cancellationToken);
}
