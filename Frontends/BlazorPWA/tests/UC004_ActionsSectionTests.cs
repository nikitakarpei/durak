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
            .Add(p => p.CanPlayAttack, false)
            .Add(p => p.CanGiveUpDefence, false));

        cut.FindAll("[data-testid='actions-section'] button").Should().BeEmpty();
    }

    [Fact]
    public void Given_PlayAttackAvailableWithoutSelection_When_ActionsSectionRenders_Then_PlayAttackIsVisibleAndDisabled()
    {
        var cut = RenderComponent<ActionsSection>(parameters => parameters
            .Add(p => p.CanPlayAttack, true)
            .Add(p => p.IsPlayAttackEnabled, false));

        cut.FindAll("[data-testid='play-attack-button']").Should().HaveCount(1);
        cut.Find("[data-testid='play-attack-button']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Given_PlayAttackAvailableWithSelection_When_PlayAttackClicked_Then_CallbackIsRaised()
    {
        var calls = 0;

        var cut = RenderComponent<ActionsSection>(parameters => parameters
            .Add(p => p.CanPlayAttack, true)
            .Add(p => p.IsPlayAttackEnabled, true)
            .Add(p => p.OnPlayAttack, () => calls++));

        cut.Find("[data-testid='play-attack-button']").Click();

        calls.Should().Be(1);
    }
}
