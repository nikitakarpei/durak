using Google.Cloud.SecretManager.V1;

namespace Secrets.Gcp;

public sealed class GoogleSecretManagerProvider(SecretManagerServiceClient client, string projectId) : ISecretsProvider
{
    public async Task<string> GetSecretAsync(string secretId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretId))
        {
            throw new ArgumentException("Secret identifier cannot be null or empty.", nameof(secretId));
        }

        try
        {
            var secretName = new SecretName(projectId, secretId);
            var request = new AccessSecretVersionRequest
            {
                Name = new SecretVersionName(projectId, secretId, "latest").ToString(),
            };

            var response = await client.AccessSecretVersionAsync(request, cancellationToken);
            return response.Payload.Data.ToStringUtf8();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve secret '{secretId}' from Google Cloud Secret Manager.",
                ex
            );
        }
    }
}
