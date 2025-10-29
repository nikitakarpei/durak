using Microsoft.Extensions.Options;
using Paseto;
using Paseto.Builder;
using Paseto.Cryptography.Key;

namespace GameService.Infrastructure;

public class GameAccessTokenServiceOptions
{
    public string SecretKey { get; set; } = string.Empty;
}

public class GameAccessTokenService : Application.IGameAccessTokenIssuer, Application.IGameAccessTokenVerifier
{
    private readonly Secrets.ISecretsProvider _secretsProvider;
    private readonly GameAccessTokenServiceOptions _options;

    private PasetoSymmetricKey? _key;
    private readonly ProtocolVersion _protocolVersion = ProtocolVersion.V4;

    public GameAccessTokenService(
        Secrets.ISecretsProvider secretsProvider,
        IOptions<GameAccessTokenServiceOptions> options
    )
    {
        _secretsProvider = secretsProvider;
        _options = options.Value;
    }

    public async Task<string> IssueAsync(string gameId, CancellationToken cancellationToken)
    {
        _key ??= await GetSecretKeyAsync(cancellationToken);

        return new PasetoBuilder()
            .Use(_protocolVersion, Purpose.Local)
            .WithKey(_key)
            .Subject(gameId)
            .IssuedAt(DateTime.UtcNow)
            .Encode();
    }

    public async Task<Application.GameAccessTokenClaims?> VerifyAsync(
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        _key ??= await GetSecretKeyAsync(cancellationToken);

        var result = new PasetoBuilder().Use(_protocolVersion, Purpose.Local).WithKey(_key).Decode(accessToken);

        if (!result.IsValid)
        {
            return null;
        }

        if (!result.Paseto.Payload.TryGetValue("sub", out var subjectValue))
        {
            return null;
        }

        var gameId = subjectValue?.ToString();
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return null;
        }

        return new Application.GameAccessTokenClaims(gameId);
    }

    private async Task<PasetoSymmetricKey> GetSecretKeyAsync(CancellationToken cancellationToken)
    {
        var secret = await _secretsProvider.GetSecretAsync(_options.SecretKey, cancellationToken);
        var key = Paserk.Decode(secret);

        if (key is PasetoSymmetricKey symmetricKey)
        {
            return symmetricKey;
        }

        throw new InvalidOperationException("Secret key is not a symmetric key.");
    }
}
