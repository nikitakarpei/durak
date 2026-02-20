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
    public void Given_PlayAttackMoveAvailableAndNoSelection_When_GameRenders_Then_TablePlayTargetHidden()
    {
        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='table-play-target']").Should().BeEmpty();
    }

    [Fact]
    public void Given_PlayAttackMoveAvailableAndSelection_When_GameRenders_Then_TablePlayTargetVisible()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();

        cut.FindAll("[data-testid='table-play-target']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_PlayAttackMoveAvailableAndSelection_When_TablePlayTargetClicked_Then_MoveIsSent()
    {
        _connectionMock
            .Setup(c => c.PlayAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();
        cut.Find("[data-testid='table-play-target']").Click();

        _connectionMock.Verify(
            c => c.PlayAttackCardAsync("token-abc", "card-1", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public void Given_PlayAttackMoveAvailableAndSelection_When_NoAccessToken_Then_MoveIsNotSent()
    {
        _connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();
        cut.Find("[data-testid='table-play-target']").Click();

        _connectionMock.Verify(
            c => c.PlayAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void Given_PlayAttackMoveAvailable_When_GameRenders_Then_PlayableHandCardsAreMarked()
    {
        var cut = RenderComponent<Game>();

        var cards = cut.FindAll("section.hand button.card-face");
        cards[0].ClassList.Should().Contain("playable");
        cards[1].ClassList.Should().NotContain("playable");
    }

    [Fact]
    public void Given_HandCardSelected_When_Clicked_Then_SelectedClassIsApplied()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();

        cut.Find("section.hand button.card-face").ClassList.Should().Contain("selected");
    }

    [Fact]
    public void Given_HandCardSelected_When_ClickedAgain_Then_SelectedClassIsRemoved()
    {
        var cut = RenderComponent<Game>();

        var card = cut.Find("section.hand button.card-face");
        card.Click();
        card.Click();

        cut.Find("section.hand button.card-face").ClassList.Should().NotContain("selected");
    }

    [Fact]
    public void Given_OpponentTurnStarts_When_StateUpdates_Then_HandCardsLosePlayableClass()
    {
        var cut = RenderComponent<Game>();

        cut.FindAll("section.hand button.card-face")[0].ClassList.Should().Contain("playable");

        _gameState.YourPossibleMoves.Clear();
        cut.Render();

        cut.FindAll("section.hand button.card-face")[0].ClassList.Should().NotContain("playable");
        cut.FindAll("section.hand button.card-face")[1].ClassList.Should().NotContain("playable");
    }

    [Fact]
    public void Given_PlayAttackMoveSent_When_StateUpdates_Then_PlayedCardMovesFromHandToTable()
    {
        _connectionMock
            .Setup(c => c.PlayAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();
        cut.Find("[data-testid='table-play-target']").Click();

        _gameState.YourHand.RemoveAll(c => c.Id == "card-1");
        _gameState.Table.Add(new TablePair(new Card("card-1", CardSuit.Hearts, CardRank.Ace), null));
        cut.Render();

        cut.FindAll("section.hand button.card-face").Should().HaveCount(1);
        var tableCardText = cut.Find("section.table-zone .pair .card-face").TextContent;
        tableCardText.Should().Contain("A");
        tableCardText.Should().Contain("♥");
        cut.Find("section.table-zone .pair .card-face").GetAttribute("data-visual-state").Should().BeNull();
    }
}
