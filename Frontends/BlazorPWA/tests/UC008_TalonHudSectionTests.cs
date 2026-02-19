using BlazorPWA.Components.Game;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;

namespace BlazorPWA.Tests;

public class UC008_TalonHudSectionTests : TestContext
{
    [Fact]
    public void Given_TrumpKnown_When_SectionRenders_Then_TrumpSlotShowsRankAndSuit()
    {
        var trump = new RegularCard { Id = "t1", Suit = CardSuit.Hearts, Rank = CardRank.Jack };

        var cut = RenderComponent<TalonHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, trump)
            .Add(p => p.TalonCount, 4));

        cut.Find("[data-testid='talon-trump']").TextContent.Should().Contain("J");
        cut.Find("[data-testid='talon-trump']").TextContent.Should().Contain("♥");
    }

    [Fact]
    public void Given_TrumpUnknown_When_SectionRenders_Then_TrumpSlotShowsPlaceholder()
    {
        var cut = RenderComponent<TalonHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, null)
            .Add(p => p.TalonCount, 4));

        cut.Find("[data-testid='talon-trump']").TextContent.Should().Contain("?");
    }

    [Fact]
    public void Given_TalonCount_When_SectionRenders_Then_DeckCountIsVisible()
    {
        var cut = RenderComponent<TalonHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, null)
            .Add(p => p.TalonCount, 2));

        cut.Find("[data-testid='talon-count']").TextContent.Should().Be("2");
    }
}
