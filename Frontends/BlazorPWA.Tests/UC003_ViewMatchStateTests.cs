using BlazorPWA.Pages;
using BlazorPWA.Services.Connection;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;
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
    public void OpponentsSectionShowsEachOpponent()
    {
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-1"] = new PlayerState("player-1", 0);
        _gameState.Players["player-2"] = new PlayerState("player-2", 1) { CardCount = 5 };
        _gameState.Players["player-3"] = new PlayerState("player-3", 2) { CardCount = 3 };

        var cut = RenderComponent<Game>();

        var opponents = cut.FindAll("div.opponent");
        opponents.Should().HaveCount(2);
        opponents[0].TextContent.Should().Contain("player-2");
        opponents[0].TextContent.Should().Contain("Seat 1");
        opponents[0].TextContent.Should().Contain("5");
        opponents[1].TextContent.Should().Contain("player-3");
        opponents[1].TextContent.Should().Contain("Seat 2");
        opponents[1].TextContent.Should().Contain("3");
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

    // ── EventLog tests ──────────────────────────────────────────────────────

    [Fact]
    public void RoundStarted_AppendsAttackerAndDefenderLog()
    {
        _gameState.LocalPlayerId = "alice";
        var evt = new RoundStarted { AttackerPlayerId = "alice", DefenderPlayerId = "bob" };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("You attacks, bob defends");
    }

    [Fact]
    public void RoundStarted_UsesPlayerIdWhenNotLocal()
    {
        _gameState.LocalPlayerId = "alice";
        var evt = new RoundStarted { AttackerPlayerId = "charlie", DefenderPlayerId = "alice" };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("charlie attacks, You defends");
    }

    [Fact]
    public void AttackCardPlayed_AppendsPlayerAndCardLog()
    {
        _gameState.LocalPlayerId = "alice";
        var playedCard = new RegularCard { Id = "c1", Suit = CardSuit.Hearts, Rank = CardRank.Ace };
        var evt = new AttackCardPlayed { PlayerId = "alice", PlayedCard = Any.Pack(playedCard) };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("You played A♥");
    }

    [Fact]
    public void AttackCardPlayed_UsesPlayerIdWhenNotLocal()
    {
        _gameState.LocalPlayerId = "alice";
        var playedCard = new RegularCard { Id = "c1", Suit = CardSuit.Clubs, Rank = CardRank.Seven };
        var evt = new AttackCardPlayed { PlayerId = "bob", PlayedCard = Any.Pack(playedCard) };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("bob played 7♣");
    }

    [Fact]
    public void AttackCardBeaten_AppendsPlayerAndBothCardsLog()
    {
        _gameState.LocalPlayerId = "alice";
        var attackCard = new RegularCard { Id = "atk-1", Suit = CardSuit.Clubs, Rank = CardRank.Seven };
        _gameState.CardsById["atk-1"] = new Card("atk-1", CardSuit.Clubs, CardRank.Seven);
        _gameState.Table.Add(new TablePair(new Card("atk-1", CardSuit.Clubs, CardRank.Seven), null));

        var defCard = new RegularCard { Id = "def-1", Suit = CardSuit.Spades, Rank = CardRank.King };
        var evt = new AttackCardBeaten { PlayerId = "alice", AttackerCardId = "atk-1", DefenderCard = Any.Pack(defCard) };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("You beat 7♣ with K♠");
    }

    [Fact]
    public void DefenceGivenUp_AppendsPlayerLog()
    {
        _gameState.LocalPlayerId = "alice";
        var evt = new DefenceGivenUp { PlayerId = "alice" };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("You gave up defence");
    }

    [Fact]
    public void PlayerTookCardsFromTable_AppendsPlayerAndCountLog()
    {
        _gameState.LocalPlayerId = "alice";
        var card1 = new RegularCard { Id = "c1", Suit = CardSuit.Hearts, Rank = CardRank.Two };
        var card2 = new RegularCard { Id = "c2", Suit = CardSuit.Clubs, Rank = CardRank.Three };
        _gameState.CardsById["c1"] = new Card("c1", CardSuit.Hearts, CardRank.Two);
        _gameState.CardsById["c2"] = new Card("c2", CardSuit.Clubs, CardRank.Three);

        var evt = new PlayerTookCardsFromTable { PlayerId = "bob" };
        evt.CardIds.Add("c1");
        evt.CardIds.Add("c2");

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("bob took 2 cards from the table");
    }

    [Fact]
    public void CardsOnTableDiscarded_AppendsDiscardedLog()
    {
        var evt = new CardsOnTableDiscarded();

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("Cards discarded — round over");
    }

    [Fact]
    public void RoundFinished_AppendsRoundFinishedLog()
    {
        var evt = new RoundFinished();

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("Round finished");
    }

    [Fact]
    public void FoolDeclared_AppendsPlayerIsFoolLog()
    {
        _gameState.LocalPlayerId = "alice";
        _gameState.Players["bob"] = new PlayerState("bob", 1);
        var evt = new FoolDeclared { PlayerId = "bob" };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("bob is the fool!");
    }

    [Fact]
    public void FoolDeclared_UsesYouWhenLocalPlayer()
    {
        _gameState.LocalPlayerId = "alice";
        _gameState.Players["alice"] = new PlayerState("alice", 0);
        var evt = new FoolDeclared { PlayerId = "alice" };

        GameStateReducer.Apply(_gameState, Any.Pack(evt));

        _gameState.EventLog.Should().ContainSingle()
            .Which.Should().Be("You is the fool!");
    }

    [Fact]
    public void EventLog_RendersInGamePage()
    {
        _gameState.EventLog.Add("You attacks, bob defends");
        _gameState.EventLog.Add("You played A♥");

        var cut = RenderComponent<Game>();

        var logSection = cut.Find("section.event-log");
        var entries = cut.FindAll("div.log-entry");
        entries.Should().HaveCount(2);
        entries[0].TextContent.Should().Contain("You attacks, bob defends");
        entries[1].TextContent.Should().Contain("You played A♥");
    }
}
