using BlazorPWA.Components.Game;
using BlazorPWA.State;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;

namespace BlazorPWA.Tests;

public class UC003_CardFaceTests : TestContext
{
    [Fact]
    public void Given_InteractiveCard_When_CardFaceRenders_Then_RendersButtonAndInvokesClick()
    {
        var wasClicked = false;
        var card = new Card("c-1", CardSuit.Hearts, CardRank.Ace);

        var cut = RenderComponent<CardFace>(parameters => parameters
            .Add(p => p.Card, card)
            .Add(p => p.IsInteractive, true)
            .Add(p => p.TestId, "card-face")
            .Add(p => p.OnClick, () => wasClicked = true));

        var element = cut.Find("[data-testid='card-face']");
        element.TagName.Should().Be("BUTTON");

        element.Click();
        wasClicked.Should().BeTrue();
    }

    [Fact]
    public void Given_StaticCard_When_CardFaceRenders_Then_RendersDivWithRankAndSuit()
    {
        var card = new Card("c-2", CardSuit.Spades, CardRank.Ten);

        var cut = RenderComponent<CardFace>(parameters => parameters
            .Add(p => p.Card, card)
            .Add(p => p.TestId, "card-face"));

        var element = cut.Find("[data-testid='card-face']");
        element.TagName.Should().Be("DIV");
        element.TextContent.Should().Contain("10");
        element.TextContent.Should().Contain("♠");
    }

    [Fact]
    public void Given_RedSuit_When_CardFaceRenders_Then_RendersRedClass()
    {
        var card = new Card("c-3", CardSuit.Diamonds, CardRank.Queen);

        var cut = RenderComponent<CardFace>(parameters => parameters
            .Add(p => p.Card, card)
            .Add(p => p.TestId, "card-face"));

        var element = cut.Find("[data-testid='card-face']");
        element.ClassList.Should().Contain("suit-red");
    }
}
