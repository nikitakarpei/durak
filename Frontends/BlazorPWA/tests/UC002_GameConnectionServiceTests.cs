using BlazorPWA.Configuration;
using BlazorPWA.Services.Api;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using FluentAssertions;
using GameService.Api.Commands;
using GameService.Api.GameEvents;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Moq;

namespace BlazorPWA.Tests;

public class UC002_GameConnectionServiceTests
{
    private readonly Mock<IGameServiceClient> _gameClientMock;
    private readonly Mock<IPlayerIdentityServiceClient> _identityClientMock;
    private readonly GameState _gameState;
    private readonly GameConnectionService _service;

    public UC002_GameConnectionServiceTests()
    {
        _gameClientMock = new Mock<IGameServiceClient>();
        _identityClientMock = new Mock<IPlayerIdentityServiceClient>();
        _gameState = new GameState();

        var settings = Options.Create(new AppSettings
        {
            Services = new AppSettings.ServicesOptions
            {
                PlayerIdentityServiceUrl = "https://identity.example.com",
                GameServiceUrl = "https://game.example.com",
            },
        });

        _service = new GameConnectionService(
            _ => _gameClientMock.Object,
            _ => _identityClientMock.Object,
            settings,
            _gameState);

        _identityClientMock
            .Setup(c => c.LoginWithCustomIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerIdentityService.Api.Data.LoginResult
            {
                PlayerId = "alice",
                PlayerIdToken = "id-token",
            });

        _gameClientMock
            .Setup(c => c.GetGameAccessTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");

        _gameClientMock
            .Setup(c => c.JoinLobbyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task WhenPlayerNotInOldEvents_JoinLobbyIsCalled()
    {
        _gameClientMock
            .Setup(c => c.GetGameEventsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EventStream(OldEventsEnded()));

        await _service.ConnectAsync("game-1", "alice", CancellationToken.None);

        _gameClientMock.Verify(
            c => c.JoinLobbyAsync("access-token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WhenLocalPlayerAlreadyInOldEvents_JoinLobbyIsNotCalled()
    {
        var playerJoined = Any.Pack(new PlayerJoined { PlayerId = "alice", Seat = 0 });

        _gameClientMock
            .Setup(c => c.GetGameEventsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EventStream(OldEvent(playerJoined), OldEventsEnded()));

        await _service.ConnectAsync("game-1", "alice", CancellationToken.None);

        _gameClientMock.Verify(
            c => c.JoinLobbyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenOtherPlayerInOldEvents_JoinLobbyIsCalled()
    {
        var playerJoined = Any.Pack(new PlayerJoined { PlayerId = "bob", Seat = 1 });

        _gameClientMock
            .Setup(c => c.GetGameEventsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EventStream(OldEvent(playerJoined), OldEventsEnded()));

        await _service.ConnectAsync("game-1", "alice", CancellationToken.None);

        _gameClientMock.Verify(
            c => c.JoinLobbyAsync("access-token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AfterConnect_StatusIsConnected()
    {
        _gameClientMock
            .Setup(c => c.GetGameEventsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EventStream(OldEventsEnded()));

        await _service.ConnectAsync("game-1", "alice", CancellationToken.None);

        _service.Status.Should().Be(ConnectionStatus.Connected);
    }

    [Fact]
    public async Task AfterConnect_AccessTokenIsSet()
    {
        _gameClientMock
            .Setup(c => c.GetGameEventsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EventStream(OldEventsEnded()));

        await _service.ConnectAsync("game-1", "alice", CancellationToken.None);

        _service.AccessToken.Should().Be("access-token");
    }

    [Fact]
    public async Task Given_OldEventsBeingApplied_When_StateChanges_Then_IsReplayingHistoryIsTrue()
    {
        var capturedDuringReplay = new List<bool>();
        _service.StateChanged += () => capturedDuringReplay.Add(_service.IsReplayingHistory);

        var playerJoined = Any.Pack(new PlayerJoined { PlayerId = "bob", Seat = 1 });
        _gameClientMock
            .Setup(c => c.GetGameEventsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EventStream(OldEvent(playerJoined), OldEventsEnded()));

        await _service.ConnectAsync("game-1", "alice", CancellationToken.None);

        capturedDuringReplay.Should().Contain(true);
    }

    [Fact]
    public async Task AfterConnect_IsReplayingHistoryIsFalse()
    {
        _gameClientMock
            .Setup(c => c.GetGameEventsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EventStream(OldEventsEnded()));

        await _service.ConnectAsync("game-1", "alice", CancellationToken.None);

        _service.IsReplayingHistory.Should().BeFalse();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async IAsyncEnumerable<GetGameEventsCommand.Types.StreamResponse> EventStream(
        params GetGameEventsCommand.Types.StreamResponse[] responses)
    {
        foreach (var r in responses)
        {
            yield return r;
            await Task.Yield();
        }
    }

    private static GetGameEventsCommand.Types.StreamResponse OldEvent(Any payload) =>
        new() { OldEvent = new() { Event = payload } };

    private static GetGameEventsCommand.Types.StreamResponse OldEventsEnded() =>
        new() { OldEventsEnded = new() };
}
