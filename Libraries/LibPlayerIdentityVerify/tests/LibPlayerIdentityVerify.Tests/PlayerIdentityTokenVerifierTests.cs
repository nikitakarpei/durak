using FluentAssertions;
using Moq;
using Paseto;
using Paseto.Builder;
using Paseto.Cryptography.Key;

namespace PlayerIdentityVerify.Tests;

public class PlayerIdentityTokenVerifierTests
{
    private readonly Mock<IPlayerIdentityServiceClientFactory> _clientFactoryMock;
    private readonly Mock<Rpc.Client.IClient> _clientMock;
    private readonly PlayerIdentityTokenVerifier _verifier;
    private readonly byte[] _seed;
    private readonly PasetoKey _secretKey;
    private readonly PasetoKey _publicKey;
    private readonly string _issuer = "https://test-issuer.example.com";

    public PlayerIdentityTokenVerifierTests()
    {
        _clientFactoryMock = new Mock<IPlayerIdentityServiceClientFactory>();
        _clientMock = new Mock<Rpc.Client.IClient>();
        _verifier = new PlayerIdentityTokenVerifier(_clientFactoryMock.Object, _issuer);

        // Generate a test key pair with a seed
        _seed = new byte[32];
        Random.Shared.NextBytes(_seed);

        var keyPair = new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Public).GenerateAsymmetricKeyPair(_seed);

        _secretKey = keyPair.SecretKey;
        _publicKey = keyPair.PublicKey;
    }

    [Fact]
    public async Task VerifyAsync_ValidToken_ReturnsVerifiedIdentity()
    {
        // Arrange
        var playerId = "test-player-123";
        var token = CreateValidToken(playerId);

        SetupClientFactoryWithPublicKey();

        // Act
        var result = await _verifier.VerifyAsync(token, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlayerId.Should().Be(playerId);
    }

    [Fact]
    public async Task VerifyAsync_InvalidSignature_ReturnsNull()
    {
        // Arrange
        var playerId = "test-player-123";
        var token = CreateValidToken(playerId);

        // Use a different key pair for verification
        var wrongSeed = new byte[32];
        Random.Shared.NextBytes(wrongSeed);
        var wrongKeyPair = new PasetoBuilder()
            .Use(ProtocolVersion.V4, Purpose.Public)
            .GenerateAsymmetricKeyPair(wrongSeed);

        SetupClientFactoryWithPublicKey(wrongKeyPair.PublicKey);

        // Act
        var result = await _verifier.VerifyAsync(token, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_MalformedToken_ReturnsNull()
    {
        // Arrange
        var malformedToken = "not-a-valid-token";

        // Act
        var result = await _verifier.VerifyAsync(malformedToken, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_EmptyToken_ReturnsNull()
    {
        // Arrange
        var emptyToken = "";

        // Act
        var result = await _verifier.VerifyAsync(emptyToken, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_TokenWithoutSubject_ReturnsNull()
    {
        // Arrange
        var token = new PasetoBuilder()
            .Use(ProtocolVersion.V4, Purpose.Public)
            .Issuer(_issuer)
            .IssuedAt(DateTime.UtcNow)
            .WithKey(_secretKey)
            .Encode();

        SetupClientFactoryWithPublicKey();

        // Act
        var result = await _verifier.VerifyAsync(token, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_CachesPublicKey_FetchesOnlyOnce()
    {
        // Arrange
        var playerId1 = "test-player-1";
        var playerId2 = "test-player-2";
        var token1 = CreateValidToken(playerId1);
        var token2 = CreateValidToken(playerId2);

        SetupClientFactoryWithPublicKey();

        // Act
        var result1 = await _verifier.VerifyAsync(token1, CancellationToken.None);
        var result2 = await _verifier.VerifyAsync(token2, CancellationToken.None);

        // Assert
        result1.Should().NotBeNull();
        result1.PlayerId.Should().Be(playerId1);
        result2.Should().NotBeNull();
        result2.PlayerId.Should().Be(playerId2);

        // Verify that UnaryCall was only made once (key was cached)
        _clientMock.Verify(
            c =>
                c.UnaryCall<
                    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request,
                    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response
                >(
                    It.IsAny<string>(),
                    It.IsAny<PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task VerifyAsync_WrongTokenVersion_ReturnsNull()
    {
        // Arrange
        var v2Token = "v2.public.somedata";

        // Act
        var result = await _verifier.VerifyAsync(v2Token, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    private string CreateValidToken(string playerId)
    {
        return new PasetoBuilder()
            .Use(ProtocolVersion.V4, Purpose.Public)
            .Subject(playerId)
            .Issuer(_issuer)
            .IssuedAt(DateTime.UtcNow)
            .WithKey(_secretKey)
            .Encode();
    }

    private void SetupClientFactoryWithPublicKey(PasetoKey? publicKey = null)
    {
        publicKey ??= _publicKey;

        var publicKeyPaserk = Paserk.Encode(publicKey, PaserkType.Public);

        var response = new PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response
        {
            Key = publicKeyPaserk,
        };

        _clientMock
            .Setup(c =>
                c.UnaryCall<
                    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request,
                    PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Response
                >(
                    "GetIdentityTokenPublicKey",
                    It.IsAny<PlayerIdentityService.Api.Commands.GetIdentityTokenPublicKeyCommand.Types.Request>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(response);

        _clientFactoryMock.Setup(f => f.Create(new Uri(_issuer))).Returns(_clientMock.Object);
    }
}
