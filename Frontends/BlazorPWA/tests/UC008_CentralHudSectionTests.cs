using BlazorPWA.Components.Game;
using Bunit;
using FluentAssertions;
using GameService.Api.Data;

namespace BlazorPWA.Tests;

public class UC008_CentralHudSectionTests : TestContext
{
    [Fact]
    public void Given_TrumpKnown_When_SectionRenders_Then_TrumpSlotShowsRankAndSuit()
    {
        var trump = new RegularCard { Id = "t1", Suit = CardSuit.Hearts, Rank = CardRank.Jack };

        var cut = RenderComponent<CentralHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, trump)
            .Add(p => p.TalonCount, 4));

        cut.Find("[data-testid='central-trump']").TextContent.Should().Contain("J");
        cut.Find("[data-testid='central-trump']").TextContent.Should().Contain("♥");
    }

    [Fact]
    public void Given_TrumpUnknown_When_SectionRenders_Then_TrumpSlotShowsPlaceholder()
    {
        var cut = RenderComponent<CentralHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, null)
            .Add(p => p.TalonCount, 4));

        cut.Find("[data-testid='central-trump']").TextContent.Should().Contain("?");
    }

    [Fact]
    public void Given_TalonCount_When_SectionRenders_Then_DeckCountIsVisible()
    {
        var cut = RenderComponent<CentralHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, null)
            .Add(p => p.TalonCount, 2));

        cut.Find("[data-testid='central-count']").TextContent.Should().Be("2");
    }

    [Fact]
    public void Given_NeutralRole_When_SectionRenders_Then_RoleIconIsHidden()
    {
        var cut = RenderComponent<CentralHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, null)
            .Add(p => p.TalonCount, 2)
            .Add(p => p.Role, CentralHudRole.Neutral));

        cut.FindAll("[data-testid='central-role']").Should().BeEmpty();
    }

    [Fact]
    public void Given_DefenderRole_When_SectionRenders_Then_ShieldIconIsVisible()
    {
        var cut = RenderComponent<CentralHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, null)
            .Add(p => p.TalonCount, 2)
            .Add(p => p.Role, CentralHudRole.Defender));

        cut.Find("[data-testid='central-role']").GetAttribute("data-role").Should().Be("defender");
    }

    [Fact]
    public void Given_AttackerRole_When_SectionRenders_Then_AttackerIconIsVisible()
    {
        var cut = RenderComponent<CentralHudSection>(parameters => parameters
            .Add(p => p.TrumpCard, null)
            .Add(p => p.TalonCount, 2)
            .Add(p => p.Role, CentralHudRole.Attacker));

        cut.Find("[data-testid='central-role']").GetAttribute("data-role").Should().Be("attacker");
    }
}
