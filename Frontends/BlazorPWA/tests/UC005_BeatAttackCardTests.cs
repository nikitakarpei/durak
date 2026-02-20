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
    public void WhenOnlyBeatAttackMovesAvailable_NoActionButtonsVisible()
    {
        var cut = RenderComponent<Game>();

        cut.FindAll("section.actions button").Should().BeEmpty();
    }

    [Fact]
    public void UndefendedTableCardsRenderAsButton()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table-zone button.card-face").Should().NotBeNull();
    }

    [Fact]
    public void ClickingTableCardAddsSelectedClass()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table-zone button.card-face").Click();

        cut.Find("section.table-zone button.card-face").ClassList.Should().Contain("selected");
    }

    [Fact]
    public void BeatAttackWithOnlyOneSelection_NotCalled()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table-zone button.card-face").Click();

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

        cut.Find("section.table-zone button.card-face").Click();
        cut.Find("section.hand button.card-face").Click();

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

        cut.Find("section.table-zone button.card-face").Click();
        cut.Find("section.hand button.card-face").Click();

        _connectionMock.Verify(
            c => c.BeatAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void SelectingHandCard_BeatableAttackCardsGetBeatableClass()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();

        cut.Find("section.table-zone button.card-face").ClassList.Should().Contain("beatable");
    }

    [Fact]
    public void SelectingAttackCard_HandCardsThatCanBeatItGetBeatableClass()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table-zone button.card-face").Click();

        cut.Find("section.hand button.card-face").ClassList.Should().Contain("beatable");
    }

    [Fact]
    public void SelectingHandCard_AttackCardWithNoBeatMoveDoesNotGetBeatableClass()
    {
        _gameState.Table.Add(new TablePair(new Card("atk-2", CardSuit.Hearts, CardRank.King), null));

        var cut = RenderComponent<Game>();

        cut.Find("section.hand button.card-face").Click();

        var tableButtons = cut.FindAll("section.table-zone button.card-face");
        tableButtons[0].ClassList.Should().Contain("beatable");    // atk-1 is beatable
        tableButtons[1].ClassList.Should().NotContain("beatable"); // atk-2 has no move
    }

    [Fact]
    public void SelectingAttackCard_HandCardWithNoBeatMoveDoesNotGetBeatableClass()
    {
        _gameState.YourHand.Add(new Card("def-2", CardSuit.Clubs, CardRank.Two));

        var cut = RenderComponent<Game>();

        cut.Find("section.table-zone button.card-face").Click();

        var handCards = cut.FindAll("section.hand button.card-face");
        handCards[0].ClassList.Should().Contain("beatable");    // def-1 can beat atk-1
        handCards[1].ClassList.Should().NotContain("beatable"); // def-2 has no move for atk-1
    }

    [Fact]
    public void WhenNoCardSelected_NoBeatableClassOnAnyCard()
    {
        var cut = RenderComponent<Game>();

        cut.Find("section.table-zone button.card-face").ClassList.Should().NotContain("beatable");
        cut.Find("section.hand button.card-face").ClassList.Should().NotContain("beatable");
    }

    [Fact]
    public void AfterStateUpdate_DefenderAppearsInChosenTablePair()
    {
        _connectionMock
            .Setup(c => c.BeatAttackCardAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<Game>();

        cut.Find("section.table-zone button.card-face").Click();
        cut.Find("section.hand button.card-face").Click();

        _gameState.Table[0] = _gameState.Table[0] with { Defence = new Card("def-1", CardSuit.Spades, CardRank.Nine) };
        _gameState.YourHand.RemoveAll(c => c.Id == "def-1");
        cut.Render();

        cut.FindAll("section.hand button.card-face").Should().BeEmpty();
        var defenceText = cut.Find("section.table-zone .pair .card-face.defence").TextContent;
        defenceText.Should().Contain("9");
        defenceText.Should().Contain("♠");
    }
}
