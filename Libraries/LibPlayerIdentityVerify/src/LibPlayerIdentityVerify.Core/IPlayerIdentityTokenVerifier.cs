namespace PlayerIdentityVerify;

public interface IPlayerIdentityTokenVerifier
{
    Task<VerifiedPlayerIdentity?> VerifyAsync(string token, CancellationToken cancellationToken);
}
