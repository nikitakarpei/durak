using BlazorPWA.Pages;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;
using Moq;

namespace BlazorPWA.Tests;

public class UC005_BeatAttackCardTests : TestContext
{
    private readonly Mock<IGameConnectionService> _connectionMock;
    private readonly GameState _gameState;

    public UC005_BeatAttackCardTests()
    {
        _connectionMock = new Mock<IGameConnectionService>();
        _gameState = new GameState();

        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Connected);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns((string?)null);
        _connectionMock.SetupGet(c => c.AccessToken).Returns("token-abc");

        _gameState.GameId = "game-123";

        _gameState.Table.Add(new TablePair(new Card("atk-1", CardSuit.Clubs, CardRank.Six), null));
        _gameState.YourHand.Add(new Card("def-1", CardSuit.Spades, CardRank.Nine));
        _gameState.YourPossibleMoves.Add(new PossibleMove(PossibleMoveType.BeatAttackCard, "def-1", "atk-1"));

        Services.AddSingleton(_connectionMock.Object);
        Services.AddSingleton(_gameState);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void WhenOnlyBeatAttackMovesAvailable_OnlyBeatAttackButtonVisible()
    {
        var cut = RenderComponent<Game>();

        var buttons = cut.FindAll("section.actions button");
        buttons.Should().HaveCount(1);
        buttons[0].TextContent.Should().Contain("Beat Attack");
    }

    [Fact]
    public void WhenNeitherCardSelected_BeatAttackButtonIsDisabled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.actions button").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void WhenOnlyHandCardSelected_BeatAttackButtonIsDisabled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card").Click();

        cut.Find("section.actions button").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void WhenOnlyAttackCardSelected_BeatAttackButtonIsDisabled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Click();

        cut.Find("section.actions button").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void WhenBothCardsSelected_BeatAttackButtonIsEnabled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Click();
        cut.Find("section.hand button.card").Click();

        cut.Find("section.actions button").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void UndefendedTableCardsRenderAsButton()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Should().NotBeNull();
    }

    [Fact]
    public void ClickingTableCardAddsSelectedClass()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Click();

        cut.Find("section.table button.card").ClassList.Should().Contain("selected");
    }

    [Fact]
    public void BeatAttackWithOnlyOneSelection_NotCalled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Click();

        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Beat Attack"))
            .Click();

        _connectionMock.Verify(
            c => c.BeatAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void BeatAttackWithBothSelections_CalledWithCorrectIds()
    {
        _connectionMock
            .Setup(c => c.BeatAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Click();
        cut.Find("section.hand button.card").Click();

        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Beat Attack"))
            .Click();

        _connectionMock.Verify(
            c => c.BeatAttackCardAsync("token-abc", "atk-1", "def-1", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void BeatAttackWithNoAccessToken_NotCalled()
    {
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Click();
        cut.Find("section.hand button.card").Click();
        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Beat Attack"))
            .Click();

        _connectionMock.Verify(
            c => c.BeatAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void AfterStateUpdate_DefenderAppearsInChosenTablePair()
    {
        _connectionMock
            .Setup(c => c.BeatAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.Find("section.table button.card").Click();
        cut.Find("section.hand button.card").Click();
        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Beat Attack"))
            .Click();

        _gameState.Table[0] = _gameState.Table[0] with { Defence = new Card("def-1", CardSuit.Spades, CardRank.Nine) };
        _gameState.YourHand.RemoveAll(c => c.Id == "def-1");
        cut.Render();

        cut.FindAll("section.hand button.card").Should().BeEmpty();
        cut.Find("section.table .pair .card.defence").TextContent.Should().Contain("9♠");
    }
}
