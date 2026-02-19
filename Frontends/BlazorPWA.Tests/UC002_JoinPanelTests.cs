using BlazorPWA.Components.Game;
using Bunit;
using FluentAssertions;

namespace BlazorPWA.Tests;

public class UC002_JoinPanelTests : TestContext
{
    [Fact]
    public void Given_DefaultState_When_JoinPanelRenders_Then_JoinControlsAreVisible()
    {
        var cut = RenderComponent<JoinPanel>(parameters => parameters
            .Add(p => p.GameId, string.Empty)
            .Add(p => p.CustomId, string.Empty)
            .Add(p => p.IsConnecting, false)
            .Add(p => p.ShowError, false));

        cut.FindAll("[data-testid='join-panel']").Should().HaveCount(1);
        cut.FindAll("[data-testid='game-id-input']").Should().HaveCount(1);
        cut.FindAll("[data-testid='custom-id-input']").Should().HaveCount(1);
        cut.FindAll("[data-testid='connect-button']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_ConnectingState_When_JoinPanelRenders_Then_ConnectButtonIsDisabled()
    {
        var cut = RenderComponent<JoinPanel>(parameters => parameters
            .Add(p => p.GameId, "game-1")
            .Add(p => p.CustomId, "player-1")
            .Add(p => p.IsConnecting, true)
            .Add(p => p.ShowError, false));

        cut.Find("[data-testid='connect-button']").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void Given_UserChangesInputs_When_ChangeEventsFire_Then_ValueCallbacksAreRaised()
    {
        var updatedGameId = string.Empty;
        var updatedCustomId = string.Empty;

        var cut = RenderComponent<JoinPanel>(parameters => parameters
            .Add(p => p.GameId, string.Empty)
            .Add(p => p.CustomId, string.Empty)
            .Add(p => p.IsConnecting, false)
            .Add(p => p.ShowError, false)
            .Add(p => p.GameIdChanged, value => updatedGameId = value)
            .Add(p => p.CustomIdChanged, value => updatedCustomId = value));

        cut.Find("[data-testid='game-id-input']").Change("game-42");
        cut.Find("[data-testid='custom-id-input']").Change("player-42");

        updatedGameId.Should().Be("game-42");
        updatedCustomId.Should().Be("player-42");
    }

    [Fact]
    public void Given_ConnectClicked_When_HandlerProvided_Then_OnConnectCallbackIsRaised()
    {
        var connectCalls = 0;

        var cut = RenderComponent<JoinPanel>(parameters => parameters
            .Add(p => p.GameId, "game-1")
            .Add(p => p.CustomId, "player-1")
            .Add(p => p.IsConnecting, false)
            .Add(p => p.ShowError, false)
            .Add(p => p.OnConnect, () => connectCalls++));

        cut.Find("[data-testid='connect-button']").Click();

        connectCalls.Should().Be(1);
    }

    [Fact]
    public void Given_ErrorState_When_JoinPanelRenders_Then_InlineErrorIsVisible()
    {
        var cut = RenderComponent<JoinPanel>(parameters => parameters
            .Add(p => p.GameId, string.Empty)
            .Add(p => p.CustomId, string.Empty)
            .Add(p => p.IsConnecting, false)
            .Add(p => p.ShowError, true)
            .Add(p => p.ErrorMessage, "Failed"));

        cut.FindAll("[data-testid='join-error']").Should().HaveCount(1);
    }
}
