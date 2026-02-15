using BlazorPWA.Pages;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;
using GameService.Api.Data.GameOptions;
using GameService.Api.GameEvents;
using Google.Protobuf.WellKnownTypes;
using Moq;

namespace BlazorPWA.Tests;

public class UC003_ViewMatchStateTests : TestContext
{
    private readonly Mock<IGameConnectionService> _connectionMock;
    private readonly GameState _gameState;

    public UC003_ViewMatchStateTests()
    {
        _connectionMock = new Mock<IGameConnectionService>();
        _gameState = new GameState();

        _connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Connected);
        _connectionMock.SetupGet(c => c.ErrorMessage).Returns((string?)null);
        _connectionMock.SetupGet(c => c.AccessToken).Returns("token-abc");

        _gameState.GameId = "game-123";

        Services.AddSingleton(_connectionMock.Object);
        Services.AddSingleton(_gameState);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void HeaderShowsGameIdTrumpAndTalon()
    {
        _gameState.TrumpCard = new RegularCard { Id = "c1", Suit = CardSuit.Hearts, Rank = CardRank.Ace };
        _gameState.TalonCount = 12;

        var cut = RenderComponent<Game>();

        var header = cut.Find("header.top-bar");
        header.TextContent.Should().Contain("game-123");
        header.TextContent.Should().Contain("A♥");
        header.TextContent.Should().Contain("12");
    }

    [Fact]
    public void OpponentsSectionShowsAllOpponentSeatsAndMarksNotJoined()
    {
        _gameState.TotalPlayerSeats = 6;
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-1"] = new PlayerState("player-1", 0);
        _gameState.Players["player-2"] = new PlayerState("player-2", 1) { CardCount = 5 };
        _gameState.Players["player-3"] = new PlayerState("player-3", 2) { CardCount = 3 };

        var cut = RenderComponent<Game>();

        var opponents = cut.FindAll("div.opponent");
        opponents.Should().HaveCount(5);
        opponents[0].TextContent.Should().Contain("player-2");
        opponents[0].TextContent.Should().Contain("Seat 1");
        opponents[0].TextContent.Should().Contain("5");
        opponents[1].TextContent.Should().Contain("player-3");
        opponents[1].TextContent.Should().Contain("Seat 2");
        opponents[1].TextContent.Should().Contain("3");
        opponents[2].TextContent.Should().Contain("Not joined");
        opponents[2].TextContent.Should().Contain("Seat 3");
        opponents[3].TextContent.Should().Contain("Not joined");
        opponents[3].TextContent.Should().Contain("Seat 4");
        opponents[4].TextContent.Should().Contain("Not joined");
        opponents[4].TextContent.Should().Contain("Seat 5");
    }

    [Fact]
    public void OpponentsSectionUsesSeatCountFromGameCreatedAmountOfPlayers()
    {
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-1"] = new PlayerState("player-1", 0);

        var created = new GameCreated();
        created.Options.Add(Any.Pack(new AmountOfPlayers { Count = 2 }));
        GameStateReducer.Apply(_gameState, Any.Pack(created));

        var cut = RenderComponent<Game>();

        var opponents = cut.FindAll("div.opponent");
        opponents.Should().HaveCount(1);
        opponents[0].TextContent.Should().Contain("Seat 1");
        opponents[0].TextContent.Should().Contain("Not joined");
    }

    [Fact]
    public void OpponentsSectionDoesNotRenderLocalSlotBeforeLocalSeatIsKnown()
    {
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-2"] = new PlayerState("player-2", 2) { CardCount = 4 };

        var created = new GameCreated();
        created.Options.Add(Any.Pack(new AmountOfPlayers { Count = 3 }));
        GameStateReducer.Apply(_gameState, Any.Pack(created));

        var cut = RenderComponent<Game>();

        var opponents = cut.FindAll("div.opponent");
        opponents.Should().HaveCount(2);
    }

    [Fact]
    public void TableSectionRendersCardPairs()
    {
        var attackCard = new Card("atk-1", CardSuit.Clubs, CardRank.Seven);
        var defenceCard = new Card("def-1", CardSuit.Spades, CardRank.King);
        _gameState.Table.Add(new TablePair(attackCard, defenceCard));
        _gameState.Table.Add(new TablePair(new Card("atk-2", CardSuit.Diamonds, CardRank.Three), null));

        var cut = RenderComponent<Game>();

        var pairs = cut.FindAll("div.pair");
        pairs.Should().HaveCount(2);
        pairs[0].TextContent.Should().Contain("7♣");
        pairs[0].TextContent.Should().Contain("K♠");
        pairs[1].TextContent.Should().Contain("3♦");
    }

    [Fact]
    public void HandSectionRendersPlayerCards()
    {
        _gameState.YourHand.Add(new Card("h1", CardSuit.Hearts, CardRank.Queen));
        _gameState.YourHand.Add(new Card("h2", CardSuit.Clubs, CardRank.Jack));

        var cut = RenderComponent<Game>();

        var handCards = cut.FindAll("section.hand button.card");
        handCards.Should().HaveCount(2);
        handCards[0].TextContent.Should().Contain("Q♥");
        handCards[1].TextContent.Should().Contain("J♣");
    }

    [Fact]
    public void WhenNotActivePlayer_NoActionButtonsVisible()
    {
        // YourPossibleMoves is empty — it is not this player's turn
        var cut = RenderComponent<Game>();

        cut.FindAll("section.actions button").Should().BeEmpty();
    }

    [Fact]
    public void RedSuitHandCard_HasSuitRedClass()
    {
        _gameState.YourHand.Add(new Card("h1", CardSuit.Hearts, CardRank.Ace));
        _gameState.YourHand.Add(new Card("h2", CardSuit.Diamonds, CardRank.King));

        var cut = RenderComponent<Game>();

        var handCards = cut.FindAll("section.hand button.card");
        handCards[0].ClassList.Should().Contain("suit-red");
        handCards[1].ClassList.Should().Contain("suit-red");
    }

    [Fact]
    public void BlackSuitHandCard_DoesNotHaveSuitRedClass()
    {
        _gameState.YourHand.Add(new Card("h1", CardSuit.Clubs, CardRank.Ace));
        _gameState.YourHand.Add(new Card("h2", CardSuit.Spades, CardRank.King));

        var cut = RenderComponent<Game>();

        var handCards = cut.FindAll("section.hand button.card");
        handCards[0].ClassList.Should().NotContain("suit-red");
        handCards[1].ClassList.Should().NotContain("suit-red");
    }

    [Fact]
    public void RedSuitTableCard_HasSuitRedClass()
    {
        _gameState.Table.Add(new TablePair(
            new Card("atk-1", CardSuit.Hearts, CardRank.Seven),
            new Card("def-1", CardSuit.Diamonds, CardRank.Jack)));

        var cut = RenderComponent<Game>();

        var pair = cut.Find("div.pair");
        pair.QuerySelector(".card:not(.defence)")!.ClassList.Should().Contain("suit-red");
        pair.QuerySelector(".card.defence")!.ClassList.Should().Contain("suit-red");
    }

    [Fact]
    public void WhenCardsAreDealtFromTalon_TalonCountDecreases()
    {
        _gameState.TalonCount = 20;
        _gameState.Players["p1"] = new PlayerState("p1", 0);

        var evt = new CardsDealtToPlayer { PlayerId = "p1" };
        evt.DealtCards.Add(Any.Pack(new RegularCard { Id = "c1", Suit = CardSuit.Hearts, Rank = CardRank.Ace }));
        evt.DealtCards.Add(Any.Pack(new RegularCard { Id = "c2", Suit = CardSuit.Spades, Rank = CardRank.King }));

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.TalonCount.Should().Be(18);
    }
}
