using Google.Cloud.SecretManager.V1;

namespace Secrets.Gcp;

public sealed class GoogleSecretManagerProvider(SecretManagerServiceClient client, string projectId) : ISecretsProvider
{
    public async Task<string> GetSecretAsync(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Secret key cannot be null or empty.", nameof(key));
        }

        try
        {
            var secretName = new SecretName(projectId, key);
            var request = new AccessSecretVersionRequest
            {
                Name = new SecretVersionName(projectId, key, "latest").ToString(),
            };

            var response = await client.AccessSecretVersionAsync(request, cancellationToken);
            return response.Payload.Data.ToStringUtf8();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve secret '{key}' from Google Cloud Secret Manager.",
                ex
            );
        }
    }
}
