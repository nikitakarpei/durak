namespace PlayerIdentityService.Application;

public interface IPlayerIdentityTokenIssuer
{
    public Task<string> IssueTokenAsync(string playerId, CancellationToken cancellationToken);
    public Task<string> GetPublicKeyAsync(CancellationToken cancellationToken);
}
