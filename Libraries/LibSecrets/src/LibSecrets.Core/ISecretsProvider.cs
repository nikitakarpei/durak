namespace Secrets;

/// <summary>
/// Interface for retrieving secrets by secret identifier.
/// </summary>
public interface ISecretsProvider
{
    Task<string> GetSecretAsync(string secretId, CancellationToken cancellationToken);
}
