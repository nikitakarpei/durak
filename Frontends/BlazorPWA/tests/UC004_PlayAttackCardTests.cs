using BlazorPWA.Pages;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;
using Moq;

namespace BlazorPWA.Tests;

public class UC004_PlayAttackCardTests : TestContext
{
    private readonly Mock<IGameConnectionService> _connectionMock;
    private readonly GameState _gameState;

    public UC004_PlayAttackCardTests()
    {
        _connectionMock = new Mock<IGameConnectionService>();
        _gameState = new GameState();

        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Connected);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns((string?)null);
        _connectionMock.SetupGet(c => c.AccessToken).Returns("token-abc");

        _gameState.GameId = "game-123";

        _gameState.YourHand.Add(new Card("card-1", CardSuit.Hearts, CardRank.Ace));
        _gameState.YourHand.Add(new Card("card-2", CardSuit.Spades, CardRank.King));
        _gameState.YourPossibleMoves.Add(new PossibleMove(PossibleMoveType.PlayAttackCard, "card-1", null));

        Services.AddSingleton(_connectionMock.Object);
        Services.AddSingleton(_gameState);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void WhenOnlyPlayAttackMovesAvailable_OnlyPlayAttackButtonVisible()
    {
        var cut = RenderComponent<Game>();

        var buttons = cut.FindAll("section.actions button");
        buttons.Should().HaveCount(1);
        buttons[0].TextContent.Should().Contain("Play Attack");
    }

    [Fact]
    public void WhenNoCardSelected_PlayAttackButtonIsDisabled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.actions button").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void WhenCardSelected_PlayAttackButtonIsEnabled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();

        cut.Find("section.actions button").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void PlayableCardsHavePlayableCssClass()
    {
        var cut = RenderComponent<Game>();

        var cards = cut.FindAll("section.hand button.card-face");
        cards[0].ClassList.Should().Contain("playable");
        cards[1].ClassList.Should().NotContain("playable");
    }

    [Fact]
    public void ClickingHandCardAddsSelectedClass()
    {
        var cut = RenderComponent<Game>();

        var card = cut.FindAll("section.hand button.card-face")[0];
        card.Click();

        cut.FindAll("section.hand button.card-face")[0].ClassList.Should().Contain("selected");
    }

    [Fact]
    public void ClickingSameCardAgainRemovesSelectedClass()
    {
        var cut = RenderComponent<Game>();

        var card = cut.FindAll("section.hand button.card-face")[0];
        card.Click();
        cut.FindAll("section.hand button.card-face")[0].Click();

        cut.FindAll("section.hand button.card-face")[0].ClassList.Should().NotContain("selected");
    }

    [Fact]
    public void PlayAttackWithNoCardSelected_NotCalled()
    {
        var cut = RenderComponent<Game>();

        cut.FindAll("section.actions button")
            .FirstOrDefault(b => b.TextContent.Contains("Play Attack"))
            ?.Click();

        _connectionMock.Verify(
            c => c.PlayAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void PlayAttackWithCardSelected_CalledWithCorrectId()
    {
        _connectionMock
            .Setup(c => c.PlayAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.FindAll("section.hand button.card-face")[0].Click();

        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Play Attack"))
            .Click();

        _connectionMock.Verify(
            c => c.PlayAttackCardAsync("token-abc", "card-1", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void PlayAttackWithNoAccessToken_NotCalled()
    {
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        var cut = RenderComponent<Game>();

        cut.FindAll("section.hand button.card-face")[0].Click();
        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Play Attack"))
            .Click();

        _connectionMock.Verify(
            c => c.PlayAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void AfterOpponentTurnStarts_HandCardsLosePlayableClass()
    {
        var cut = RenderComponent<Game>();

        cut.FindAll("section.hand button.card-face")[0].ClassList.Should().Contain("playable");

        // Simulate OpponentTurnStarted state update
        _gameState.YourPossibleMoves.Clear();
        cut.Render();

        cut.FindAll("section.hand button.card-face")[0].ClassList.Should().NotContain("playable");
        cut.FindAll("section.hand button.card-face")[1].ClassList.Should().NotContain("playable");
    }

    [Fact]
    public void AfterStateUpdate_PlayedCardMovesFromHandToTable()
    {
        _connectionMock
            .Setup(c => c.PlayAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.FindAll("section.hand button.card-face")[0].Click();
        cut.FindAll("section.actions button")
            .First(b => b.TextContent.Contains("Play Attack"))
            .Click();

        _gameState.YourHand.RemoveAll(c => c.Id == "card-1");
        _gameState.Table.Add(new TablePair(new Card("card-1", CardSuit.Hearts, CardRank.Ace), null));
        cut.Render();

        cut.FindAll("section.hand button.card-face").Should().HaveCount(1);
        var tableCardText = cut.Find("section.table .pair .card-face").TextContent;
        tableCardText.Should().Contain("A");
        tableCardText.Should().Contain("♥");
    }
}
