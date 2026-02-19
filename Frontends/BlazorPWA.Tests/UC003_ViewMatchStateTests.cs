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
    public void Given_TrumpCardKnown_When_MatchRenders_Then_TrumpCardIsVisible()
    {
        _gameState.TrumpCard = new RegularCard { Id = "c1", Suit = CardSuit.Hearts, Rank = CardRank.Ace };

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='trump-card']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_PositiveTalonCount_When_MatchRenders_Then_DeckBackAndTalonCountAreVisible()
    {
        _gameState.TalonCount = 7;

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='deck-back-card']").Should().HaveCount(1);
        cut.Find("[data-testid='talon-count']").GetAttribute("data-count").Should().Be("7");
    }

    [Fact]
    public void Given_ZeroTalonCount_When_MatchRenders_Then_DeckBackIsHidden()
    {
        _gameState.TalonCount = 0;

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='deck-back-card']").Should().BeEmpty();
    }

    [Fact]
    public void Given_ConfiguredSeatsAndJoinedOpponents_When_MatchRenders_Then_AllOpponentSlotsAreShown()
    {
        _gameState.TotalPlayerSeats = 6;
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-1"] = new PlayerState("player-1", 0);
        _gameState.Players["player-2"] = new PlayerState("player-2", 1) { CardCount = 5 };
        _gameState.Players["player-3"] = new PlayerState("player-3", 2) { CardCount = 3 };

        var cut = RenderComponent<Game>();

        var seats = cut.FindAll("[data-testid='opponent-seat']");
        seats.Should().HaveCount(5);
        seats.Count(s => s.GetAttribute("data-joined") == "true").Should().Be(2);
        seats.Count(s => s.GetAttribute("data-joined") == "false").Should().Be(3);
        cut.Markup.Should().NotContain("player-1");
    }

    [Fact]
    public void Given_OpponentWithManyCards_When_MatchRenders_Then_VisualCardBacksAreCappedAtEight()
    {
        _gameState.TotalPlayerSeats = 2;
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-1"] = new PlayerState("player-1", 0);
        _gameState.Players["player-2"] = new PlayerState("player-2", 1) { CardCount = 11 };

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='opponent-card-back'][data-seat='1']").Should().HaveCount(8);
    }

    [Fact]
    public void Given_GameCreatedWithAmountOfPlayers_When_MatchRenders_Then_OpponentSeatCountFollowsConfiguration()
    {
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-1"] = new PlayerState("player-1", 0);

        var created = new GameCreated();
        created.Options.Add(Any.Pack(new AmountOfPlayers { Count = 2 }));
        GameStateReducer.Apply(_gameState, Any.Pack(created));

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='opponent-seat']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_LocalPlayerKnownWithoutSeat_When_MatchRenders_Then_OneSeatIsReservedForLocalPlayer()
    {
        _gameState.LocalPlayerId = "player-1";
        _gameState.Players["player-2"] = new PlayerState("player-2", 2) { CardCount = 4 };

        var created = new GameCreated();
        created.Options.Add(Any.Pack(new AmountOfPlayers { Count = 3 }));
        GameStateReducer.Apply(_gameState, Any.Pack(created));

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='opponent-seat']").Should().HaveCount(2);
    }

    [Fact]
    public void Given_TablePairsExist_When_MatchRenders_Then_AllPairsAreVisible()
    {
        _gameState.Table.Add(new TablePair(
            new Card("atk-1", CardSuit.Clubs, CardRank.Seven),
            new Card("def-1", CardSuit.Spades, CardRank.King)));
        _gameState.Table.Add(new TablePair(new Card("atk-2", CardSuit.Diamonds, CardRank.Three), null));

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='table-pair']").Should().HaveCount(2);
        cut.Find("[data-testid='table-defence-atk-1']").Children.Should().HaveCount(2);
        cut.Find("[data-testid='table-defence-atk-2']").Children.Should().BeEmpty();
    }

    [Fact]
    public void Given_HandCardsExist_When_MatchRenders_Then_AllHandCardsAreVisible()
    {
        _gameState.YourHand.Add(new Card("h1", CardSuit.Hearts, CardRank.Queen));
        _gameState.YourHand.Add(new Card("h2", CardSuit.Clubs, CardRank.Jack));

        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='hand-card-h1']").Should().HaveCount(1);
        cut.FindAll("[data-testid='hand-card-h2']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_NoPossibleMoves_When_MatchRenders_Then_NoActionButtonsAreVisible()
    {
        var cut = RenderComponent<Game>();

        cut.FindAll("[data-testid='actions-section'] button").Should().BeEmpty();
    }

    [Fact]
    public void Given_CardsDealtEvent_When_ReducerApplies_Then_TalonCountDecreases()
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
