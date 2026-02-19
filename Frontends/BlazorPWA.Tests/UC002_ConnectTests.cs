using BlazorPWA.Pages;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using Moq;

namespace BlazorPWA.Tests;

public class UC002_ConnectTests : TestContext
{
    private readonly Mock<IGameConnectionService> _connectionMock;
    private readonly GameState _gameState;

    public UC002_ConnectTests()
    {
        _connectionMock = new Mock<IGameConnectionService>();
        _gameState = new GameState();

        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Idle);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns((string?)null);
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        Services.AddSingleton(_connectionMock.Object);
        Services.AddSingleton(_gameState);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void JoinScreenShownWhenStatusIsIdle()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Idle);

        var cut = RenderComponent<Game>();

        cut.Find("[data-testid='join-panel']").Should().NotBeNull();
        cut.Find("[data-testid='connect-button']").Should().NotBeNull();
    }

    [Fact]
    public void ConnectButtonDisabledWhileConnecting()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Idle);
        var connectPending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectionMock
            .Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(connectPending.Task);

        var cut = RenderComponent<Game>();

        var gameIdInput = cut.Find("[data-testid='game-id-input']");
        gameIdInput.Change("test-game");

        var connectButton = cut.Find("[data-testid='connect-button']");
        connectButton.Click();

        cut.Find("[data-testid='connect-button']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void MatchScreenShownWhenConnectedAndGameIdSet()
    {
        _gameState.GameId = "game-123";
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Connected);

        var cut = RenderComponent<Game>();

        cut.Find("[data-testid='match-panel']").Should().NotBeNull();
    }

    [Fact]
    public void ErrorMessageShownWhenStatusIsError()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Error);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns("Connection failed");

        var cut = RenderComponent<Game>();

        cut.Find("[data-testid='join-error']").TextContent.Should().Be("Connection failed");
    }

    [Fact]
    public void EmptyGameId_ClickConnect_ConnectAsyncNotCalled()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Idle);

        var cut = RenderComponent<Game>();

        cut.Find("[data-testid='connect-button']").Click();

        _connectionMock.Verify(
            c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void GameIdAndCustomIdProvided_ClickConnect_ConnectAsyncCalledWithTrimmedValues()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Idle);
        _connectionMock
            .Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.Find("[data-testid='game-id-input']").Change("  game-123  ");
        cut.Find("[data-testid='custom-id-input']").Change("  player-xyz  ");

        cut.Find("[data-testid='connect-button']").Click();

        _connectionMock.Verify(
            c => c.ConnectAsync("game-123", "player-xyz", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
