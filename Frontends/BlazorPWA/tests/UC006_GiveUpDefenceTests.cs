using BlazorPWA.Pages;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;
using Moq;

namespace BlazorPWA.Tests;

public class UC006_GiveUpDefenceTests : TestContext
{
    private readonly Mock<IGameConnectionService> _connectionMock;
    private readonly GameState _gameState;

    public UC006_GiveUpDefenceTests()
    {
        _connectionMock = new Mock<IGameConnectionService>();
        _gameState = new GameState();

        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Connected);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns((string?)null);
        _connectionMock.SetupGet(c => c.AccessToken).Returns("token-abc");

        _gameState.GameId = "game-123";
        _gameState.Table.Add(new TablePair(new Card("atk-1", CardSuit.Clubs, CardRank.Six), null));
        _gameState.Table.Add(new TablePair(new Card("atk-2", CardSuit.Hearts, CardRank.Seven), null));
        _gameState.YourPossibleMoves.Add(new PossibleMove(PossibleMoveType.GiveUpDefence, null, null));

        Services.AddSingleton(_connectionMock.Object);
        Services.AddSingleton(_gameState);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void WhenOnlyGiveUpDefenceMoveAvailable_OnlyGiveUpDefenceButtonVisible()
    {
        var cut = RenderComponent<Game>();

        var buttons = cut.FindAll("section.actions button");
        buttons.Should().HaveCount(1);
        buttons[0].TextContent.Should().Contain("Give Up Defence");
    }

    [Fact]
    public void ClickGiveUpDefence_GiveUpDefenceAsyncCalled()
    {
        _connectionMock
            .Setup(c => c.GiveUpDefenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Give Up Defence"))
            .Click();

        _connectionMock.Verify(
            c => c.GiveUpDefenceAsync("token-abc", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void ClickGiveUpDefenceWithoutAccessToken_NotCalled()
    {
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        var cut = RenderComponent<Game>();

        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Give Up Defence"))
            .Click();

        _connectionMock.Verify(
            c => c.GiveUpDefenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void AfterGivingUpDefence_AttackerCardsAppearInDefenderHand()
    {
        var cut = RenderComponent<Game>();

        // Simulate state update from PlayerTookCardsFromTable event:
        // table cards are moved to the local player's hand and table is cleared
        _gameState.YourHand.Add(new Card("atk-1", CardSuit.Clubs, CardRank.Six));
        _gameState.YourHand.Add(new Card("atk-2", CardSuit.Hearts, CardRank.Seven));
        _gameState.Table.Clear();
        cut.Render();

        var handCards = cut.FindAll("section.hand button.card");
        handCards.Should().HaveCount(2);
        handCards.Select(c => c.TextContent).Should().Contain(s => s.Contains("6♣"));
        handCards.Select(c => c.TextContent).Should().Contain(s => s.Contains("7♥"));
    }
}
