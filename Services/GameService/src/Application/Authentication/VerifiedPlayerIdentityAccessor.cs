namespace GameService.Application;

public interface IVerifiedPlayerIdentityProvider
{
    public PlayerIdentityVerify.VerifiedPlayerIdentity? Identity { get; }
}

public class VerifiedPlayerIdentityAccessor : IVerifiedPlayerIdentityProvider
{
    public PlayerIdentityVerify.VerifiedPlayerIdentity? Identity { get; set; }
}
