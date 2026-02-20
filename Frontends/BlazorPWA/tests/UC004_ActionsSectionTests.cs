using BlazorPWA.Components.Game;
using Bunit;
using FluentAssertions;

namespace BlazorPWA.Tests;

public class UC004_ActionsSectionTests : TestContext
{
    [Fact]
    public void Given_NoAvailableMoves_When_ActionsSectionRenders_Then_NoButtonsAreVisible()
    {
        var cut = RenderComponent<ActionsSection>(parameters => parameters
            .Add(p => p.CanGiveUpDefence, false));

        cut.FindAll("[data-testid='actions-section'] button").Should().BeEmpty();
    }

    [Fact]
    public void Given_GiveUpAvailable_When_ActionsSectionRenders_Then_GiveUpButtonIsVisible()
    {
        var cut = RenderComponent<ActionsSection>(parameters => parameters
            .Add(p => p.CanGiveUpDefence, true));

        cut.FindAll("[data-testid='give-up-defence-button']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_GiveUpAvailable_When_GiveUpClicked_Then_CallbackIsRaised()
    {
        var calls = 0;

        var cut = RenderComponent<ActionsSection>(parameters => parameters
            .Add(p => p.CanGiveUpDefence, true)
            .Add(p => p.OnGiveUpDefence, () => calls++));

        cut.Find("[data-testid='give-up-defence-button']").Click();

        calls.Should().Be(1);
    }
}
