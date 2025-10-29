using Paseto;
using Paseto.Builder;
using Paseto.Cryptography.Key;

namespace PlayerIdentityVerify;

public class PlayerIdentityTokenVerifier(IPlayerIdentityServiceClientFactory clientFactory, string serviceUrl)
    : IPlayerIdentityTokenVerifier
{
    private PasetoKey? _cachedPublicKey;
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public async Task<VerifiedPlayerIdentity?> VerifyAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var publicKey = await GetOrFetchPublicKeyAsync(cancellationToken);
            if (publicKey == null)
            {
                return null;
            }

            var validationParams = new PasetoTokenValidationParameters { ValidateLifetime = true };

            var result = new PasetoBuilder()
                .Use(ProtocolVersion.V4, Purpose.Public)
                .WithKey(publicKey)
                .Decode(token, validationParams);

            if (!result.IsValid)
            {
                return null;
            }

            if (!result.Paseto.Payload.TryGetValue("sub", out var subjectValue))
            {
                return null;
            }

            var playerId = subjectValue?.ToString();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return null;
            }

            return new VerifiedPlayerIdentity(playerId);
        }
        catch
        {
            return null;
        }
    }

    private async Task<PasetoKey?> GetOrFetchPublicKeyAsync(CancellationToken cancellationToken)
    {
        // Wait for any concurrent fetch to complete
        await _fetchSemaphore.WaitAsync(cancellationToken);
        try
        {
            // If we already have the key, return it
            if (_cachedPublicKey != null)
            {
                return _cachedPublicKey;
            }

            // Fetch the public key from the service
            using var client = clientFactory.Create(new Uri(serviceUrl));

            var request = new PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request();
            var response = await client.UnaryCall<
                PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request,
                PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response
            >("GetIdentityTokenPublicKey", request, cancellationToken);

            if (string.IsNullOrWhiteSpace(response.Key))
            {
                return null;
            }

            var publicKey = Paserk.Decode(response.Key);

            _cachedPublicKey = publicKey;

            return publicKey;
        }
        finally
        {
            _fetchSemaphore.Release();
        }
    }
}
