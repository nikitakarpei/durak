using BlazorPWA.Components.Game;
using Bunit;
using FluentAssertions;

namespace BlazorPWA.Tests;

public class UC003_OpponentsSectionTests : TestContext
{
    [Fact]
    public void Given_MixedJoinedAndUnjoinedSeats_When_SectionRenders_Then_AllSeatsAndJoinStatesAreVisible()
    {
        var seats = new List<OpponentSeatViewModel>
        {
            new(1, true, 5),
            new(2, true, 3),
            new(3, false, 0),
            new(4, false, 0),
            new(5, false, 0),
        };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        var renderedSeats = cut.FindAll("[data-testid='opponent-seat']");
        renderedSeats.Should().HaveCount(5);
        renderedSeats.Count(s => s.GetAttribute("data-joined") == "true").Should().Be(2);
        renderedSeats.Count(s => s.GetAttribute("data-joined") == "false").Should().Be(3);
    }

    [Fact]
    public void Given_JoinedSeatWithHighCardCount_When_SectionRenders_Then_CardBacksAreCappedAtEight()
    {
        var seats = new List<OpponentSeatViewModel>
        {
            new(1, true, 11),
        };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        cut.FindAll("[data-testid='opponent-card-back'][data-seat='1']").Should().HaveCount(8);
    }

    [Fact]
    public void Given_UnjoinedSeat_When_SectionRenders_Then_NoCardBacksAreRenderedForThatSeat()
    {
        var seats = new List<OpponentSeatViewModel>
        {
            new(2, false, 0),
        };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        cut.FindAll("[data-testid='opponent-card-back'][data-seat='2']").Should().BeEmpty();
    }
}
