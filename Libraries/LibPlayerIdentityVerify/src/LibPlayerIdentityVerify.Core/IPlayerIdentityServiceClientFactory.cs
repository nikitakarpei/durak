namespace PlayerIdentityVerify;

public interface IPlayerIdentityServiceClientFactory
{
    Rpc.Client.IClient Create(Uri serviceUrl);
}
