using BlazorPWA.Components;
using BlazorPWA.Pages;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using Moq;

namespace BlazorPWA.Tests;

public class UC007_ErrorHandlingTests : TestContext
{
    private readonly Mock<IGameConnectionService> _connectionMock;
    private readonly GameState _gameState;

    public UC007_ErrorHandlingTests()
    {
        _connectionMock = new Mock<IGameConnectionService>();
        _gameState = new GameState();

        Services.AddSingleton(_connectionMock.Object);
        Services.AddSingleton(_gameState);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ErrorToastRenderedWithMessageWhenStatusIsError()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Error);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns("Something went wrong");
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        var cut = RenderComponent<ErrorToast>();

        cut.Find("div.toast").TextContent.Should().Be("Something went wrong");
    }

    [Fact]
    public void JoinScreenShowsInlineErrorWhenStatusIsError()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Error);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns("Connection refused");
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        var cut = RenderComponent<Game>();

        cut.Find("div.error").TextContent.Should().Be("Connection refused");
    }

    [Fact]
    public void MatchScreenShowsErrorToastWhenStatusIsError()
    {
        _gameState.GameId = "game-123";
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Error);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns("Move failed");
        _connectionMock.SetupGet(c => c.AccessToken).Returns("token-abc");

        var cut = RenderComponent<Game>();

        cut.Find("div.toast").TextContent.Should().Be("Move failed");
    }

    [Fact]
    public void ErrorStateWithNoGameId_KeepsJoinScreenVisible()
    {
        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Error);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns("Connection refused");
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        var cut = RenderComponent<Game>();

        cut.Find("h1").TextContent.Should().Be("Join Game");
        cut.FindAll("section.match").Should().BeEmpty();
    }
}
