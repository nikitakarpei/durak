namespace Secrets;

/// <summary>
/// Interface for retrieving secrets by key.
/// </summary>
public interface ISecretsProvider
{
    Task<string> GetSecretAsync(string key, CancellationToken cancellationToken);
}
