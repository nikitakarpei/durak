using BlazorPWA.Components.Game;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;

namespace BlazorPWA.Tests;

public class UC005_TableSectionTests : TestContext
{
    [Fact]
    public void Given_OpenAndDefendedPairs_When_TableSectionRenders_Then_PairsRenderWithExpectedInteractivity()
    {
        var pairs = new List<TablePair>
        {
            new(new Card("atk-1", CardSuit.Clubs, CardRank.Six), null),
            new(new Card("atk-2", CardSuit.Hearts, CardRank.King), new Card("def-2", CardSuit.Spades, CardRank.Ace)),
        };

        var cut = RenderComponent<TableSection>(parameters => parameters
            .Add(p => p.Pairs, pairs)
            .Add(p => p.BeatableAttackCardIds, new HashSet<string>()));

        cut.FindAll("[data-testid='table-pair']").Should().HaveCount(2);
        cut.Find("[data-testid='table-attack-atk-1']").TagName.Should().Be("BUTTON");
        cut.Find("[data-testid='table-attack-atk-2']").TagName.Should().Be("DIV");
    }

    [Fact]
    public void Given_SelectedAndBeatableAttack_When_TableSectionRenders_Then_OpenAttackHasSelectedAndBeatableClasses()
    {
        var pairs = new List<TablePair>
        {
            new(new Card("atk-1", CardSuit.Clubs, CardRank.Six), null),
        };

        var cut = RenderComponent<TableSection>(parameters => parameters
            .Add(p => p.Pairs, pairs)
            .Add(p => p.SelectedAttackCardId, "atk-1")
            .Add(p => p.BeatableAttackCardIds, new HashSet<string> { "atk-1" }));

        var attack = cut.Find("[data-testid='table-attack-atk-1']");
        attack.ClassList.Should().Contain("selected");
        attack.ClassList.Should().Contain("beatable");
    }

    [Fact]
    public void Given_BeatableAttackExists_When_TableSectionRenders_Then_NonBeatableCardsAreMarkedUnbeatable()
    {
        var pairs = new List<TablePair>
        {
            new(new Card("atk-1", CardSuit.Clubs, CardRank.Six), null),
            new(new Card("atk-2", CardSuit.Hearts, CardRank.King), null),
        };

        var cut = RenderComponent<TableSection>(parameters => parameters
            .Add(p => p.Pairs, pairs)
            .Add(p => p.BeatableAttackCardIds, new HashSet<string> { "atk-1" }));

        cut.Find("[data-testid='table-attack-atk-1']").GetAttribute("data-visual-state").Should().BeNull();
        cut.Find("[data-testid='table-attack-atk-2']").GetAttribute("data-visual-state").Should().Be("unbeatable");
    }

    [Fact]
    public void Given_DefendedPair_When_TableSectionRenders_Then_PairHasBeatenClass()
    {
        var pairs = new List<TablePair>
        {
            new(new Card("atk-1", CardSuit.Clubs, CardRank.Six), new Card("def-1", CardSuit.Spades, CardRank.Ace)),
        };

        var cut = RenderComponent<TableSection>(parameters => parameters
            .Add(p => p.Pairs, pairs)
            .Add(p => p.BeatableAttackCardIds, new HashSet<string>()));

        cut.Find("[data-testid='table-pair']").ClassList.Should().Contain("beaten");
    }

    [Fact]
    public void Given_OpenAttackPair_When_TableSectionRenders_Then_PairDoesNotHaveBeatenClass()
    {
        var pairs = new List<TablePair>
        {
            new(new Card("atk-1", CardSuit.Clubs, CardRank.Six), null),
        };

        var cut = RenderComponent<TableSection>(parameters => parameters
            .Add(p => p.Pairs, pairs)
            .Add(p => p.BeatableAttackCardIds, new HashSet<string>()));

        cut.Find("[data-testid='table-pair']").ClassList.Should().NotContain("beaten");
    }

    [Fact]
    public void Given_OpenAttackCard_When_Clicked_Then_AttackSelectedCallbackIsRaisedWithCardId()
    {
        var selectedAttack = string.Empty;
        var pairs = new List<TablePair>
        {
            new(new Card("atk-1", CardSuit.Clubs, CardRank.Six), null),
        };

        var cut = RenderComponent<TableSection>(parameters => parameters
            .Add(p => p.Pairs, pairs)
            .Add(p => p.BeatableAttackCardIds, new HashSet<string>())
            .Add(p => p.OnAttackCardSelected, id => selectedAttack = id));

        cut.Find("[data-testid='table-attack-atk-1']").Click();

        selectedAttack.Should().Be("atk-1");
    }
}
