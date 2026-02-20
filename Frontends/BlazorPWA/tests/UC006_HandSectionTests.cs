using BlazorPWA.Components.Game;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;

namespace BlazorPWA.Tests;

public class UC006_HandSectionTests : TestContext
{
    [Fact]
    public void Given_EmptyHand_When_SectionRenders_Then_NoCardsAreVisible()
    {
        var cut = RenderComponent<HandSection>(parameters => parameters
            .Add(p => p.Cards, new List<Card>())
            .Add(p => p.PlayableCardIds, new HashSet<string>())
            .Add(p => p.BeatableHandCardIds, new HashSet<string>()));

        cut.FindAll("[data-testid^='hand-card-']").Should().BeEmpty();
    }

    [Fact]
    public void Given_PopulatedHand_When_SectionRenders_Then_AllCardsAreVisible()
    {
        var cards = new List<Card>
        {
            new("h1", CardSuit.Hearts, CardRank.Queen),
            new("h2", CardSuit.Clubs, CardRank.Jack),
        };

        var cut = RenderComponent<HandSection>(parameters => parameters
            .Add(p => p.Cards, cards)
            .Add(p => p.PlayableCardIds, new HashSet<string>())
            .Add(p => p.BeatableHandCardIds, new HashSet<string>()));

        cut.FindAll("[data-testid='hand-card-h1']").Should().HaveCount(1);
        cut.FindAll("[data-testid='hand-card-h2']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_PopulatedHand_When_SectionRenders_Then_ScrollContainerIsPresent()
    {
        var cards = new List<Card>
        {
            new("h1", CardSuit.Hearts, CardRank.Queen),
        };

        var cut = RenderComponent<HandSection>(parameters => parameters
            .Add(p => p.Cards, cards)
            .Add(p => p.PlayableCardIds, new HashSet<string>())
            .Add(p => p.BeatableHandCardIds, new HashSet<string>()));

        cut.FindAll("[data-testid='hand-scroll']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_PlayableCardExists_When_SectionRenders_Then_NonPlayableCardsRemainVisuallyActive()
    {
        var cards = new List<Card>
        {
            new("h1", CardSuit.Hearts, CardRank.Queen),
            new("h2", CardSuit.Clubs, CardRank.Jack),
        };

        var cut = RenderComponent<HandSection>(parameters => parameters
            .Add(p => p.Cards, cards)
            .Add(p => p.PlayableCardIds, new HashSet<string> { "h1" })
            .Add(p => p.BeatableHandCardIds, new HashSet<string>()));

        cut.Find("[data-testid='hand-card-h1']").GetAttribute("data-visual-state").Should().BeNull();
        cut.Find("[data-testid='hand-card-h2']").GetAttribute("data-visual-state").Should().BeNull();
    }

    [Fact]
    public void Given_SelectableCard_When_Clicked_Then_OnCardSelectedCallbackIsRaisedWithCardId()
    {
        var selectedCard = string.Empty;
        var cards = new List<Card> { new("h1", CardSuit.Hearts, CardRank.Queen) };

        var cut = RenderComponent<HandSection>(parameters => parameters
            .Add(p => p.Cards, cards)
            .Add(p => p.PlayableCardIds, new HashSet<string>())
            .Add(p => p.BeatableHandCardIds, new HashSet<string>())
            .Add(p => p.OnCardSelected, id => selectedCard = id));

        cut.Find("[data-testid='hand-card-h1']").Click();

        selectedCard.Should().Be("h1");
    }
}
