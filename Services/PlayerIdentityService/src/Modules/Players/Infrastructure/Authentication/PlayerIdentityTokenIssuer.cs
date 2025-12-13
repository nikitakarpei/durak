using Microsoft.Extensions.Options;
using Paseto;
using Paseto.Builder;
using Paseto.Cryptography.Key;
using PlayerIdentityService.Modules.Players.Application.Authentication;

namespace PlayerIdentityService.Modules.Players.Infrastructure.Authentication;

public class PlayerIdentityTokenIssuerOptions
{
    public string SeedSecretId { get; set; } = string.Empty;
}

public class PlayerIdentityTokenIssuer : IPlayerIdentityTokenIssuer
{
    private readonly Secrets.ISecretsProvider _secretsProvider;
    private readonly PlayerIdentityTokenIssuerOptions _options;

    private PasetoAsymmetricKeyPair? _pasetoKeyPair;
    private readonly ProtocolVersion _protocolVersion = ProtocolVersion.V4;

    public PlayerIdentityTokenIssuer(
        Secrets.ISecretsProvider secretsProvider,
        IOptions<PlayerIdentityTokenIssuerOptions> options
    )
    {
        _secretsProvider = secretsProvider;
        _options = options.Value;
    }

    public async Task<string> IssueTokenAsync(string playerId, CancellationToken cancellationToken)
    {
        _pasetoKeyPair ??= await GenerateKeyPairAsync(cancellationToken);

        return new PasetoBuilder()
            .Use(_protocolVersion, Purpose.Public)
            .Subject(playerId)
            .IssuedAt(DateTime.UtcNow)
            .WithKey(_pasetoKeyPair.SecretKey)
            .Encode();
    }

    public async Task<string> GetPublicKeyAsync(CancellationToken cancellationToken)
    {
        _pasetoKeyPair ??= await GenerateKeyPairAsync(cancellationToken);

        return Paserk.Encode(_pasetoKeyPair.PublicKey, PaserkType.Public);
    }

    private async Task<PasetoAsymmetricKeyPair> GenerateKeyPairAsync(CancellationToken cancellationToken)
    {
        var seed = await _secretsProvider.GetSecretAsync(_options.SeedSecretId, cancellationToken);
        var seedBytes = Convert.FromHexString(seed);

        return new PasetoBuilder().Use(_protocolVersion, Purpose.Public).GenerateAsymmetricKeyPair(seedBytes);
    }
}
