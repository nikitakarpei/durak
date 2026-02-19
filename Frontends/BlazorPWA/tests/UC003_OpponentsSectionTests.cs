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

    [Fact]
    public void Given_OneOpponentSeat_When_SectionRenders_Then_SeatIsInTier1Only()
    {
        var seats = new List<OpponentSeatViewModel> { new(1, true, 3) };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        cut.FindAll("[data-testid='opponent-seat'][data-tier='1']").Should().HaveCount(1);
        cut.FindAll("[data-testid='opponent-seat'][data-tier='2']").Should().BeEmpty();
    }

    [Fact]
    public void Given_TwoOpponentSeats_When_SectionRenders_Then_BothSeatsAreInTier1Only()
    {
        var seats = new List<OpponentSeatViewModel> { new(1, true, 3), new(2, false, 0) };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        cut.FindAll("[data-testid='opponent-seat'][data-tier='1']").Should().HaveCount(2);
        cut.FindAll("[data-testid='opponent-seat'][data-tier='2']").Should().BeEmpty();
    }

    [Fact]
    public void Given_ThreeOpponentSeats_When_SectionRenders_Then_AllThreeSeatsAreInTier1Only()
    {
        var seats = new List<OpponentSeatViewModel>
        {
            new(1, true, 3), new(2, false, 0), new(3, false, 0),
        };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        cut.FindAll("[data-testid='opponent-seat'][data-tier='1']").Should().HaveCount(3);
        cut.FindAll("[data-testid='opponent-seat'][data-tier='2']").Should().BeEmpty();
    }

    [Fact]
    public void Given_FourOpponentSeats_When_SectionRenders_Then_TwoSeatsInEachTier()
    {
        var seats = new List<OpponentSeatViewModel>
        {
            new(1, true, 3), new(2, false, 0), new(3, false, 0), new(4, false, 0),
        };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        cut.FindAll("[data-testid='opponent-seat'][data-tier='1']").Should().HaveCount(2);
        cut.FindAll("[data-testid='opponent-seat'][data-tier='2']").Should().HaveCount(2);
    }

    [Fact]
    public void Given_FiveOpponentSeats_When_SectionRenders_Then_ThreeSeatsInTier1AndTwoInTier2()
    {
        var seats = new List<OpponentSeatViewModel>
        {
            new(1, true, 3), new(2, false, 0), new(3, false, 0), new(4, false, 0), new(5, false, 0),
        };

        var cut = RenderComponent<OpponentsSection>(parameters => parameters.Add(p => p.Seats, seats));

        cut.FindAll("[data-testid='opponent-seat'][data-tier='1']").Should().HaveCount(3);
        cut.FindAll("[data-testid='opponent-seat'][data-tier='2']").Should().HaveCount(2);
    }
}
