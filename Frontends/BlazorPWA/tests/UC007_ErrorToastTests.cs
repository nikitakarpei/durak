using BlazorPWA.Components;
using BlazorPWA.Services.Connection;
using Bunit;
using FluentAssertions;
using Moq;

namespace BlazorPWA.Tests;

public class UC007_ErrorToastTests : TestContext
{
    [Fact]
    public void Given_ErrorStatusAndMessage_When_ErrorToastRenders_Then_ToastIsVisible()
    {
        var connectionMock = new Mock<IGameConnectionService>();
        connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Error);
        connectionMock.SetupGet(c => c.ErrorMessage).Returns("Something went wrong");
        connectionMock.SetupGet(c => c.AccessToken).Returns((string?)null);

        Services.AddSingleton(connectionMock.Object);

        var cut = RenderComponent<ErrorToast>();

        cut.FindAll("[data-testid='error-toast']").Should().HaveCount(1);
    }

    [Fact]
    public void Given_NonErrorStatus_When_ErrorToastRenders_Then_ToastIsHidden()
    {
        var connectionMock = new Mock<IGameConnectionService>();
        connectionMock.SetupGet(c => c.Status).Returns(ConnectionStatus.Connected);
        connectionMock.SetupGet(c => c.ErrorMessage).Returns("Should not show");
        connectionMock.SetupGet(c => c.AccessToken).Returns("token-1");

        Services.AddSingleton(connectionMock.Object);

        var cut = RenderComponent<ErrorToast>();

        cut.FindAll("[data-testid='error-toast']").Should().BeEmpty();
    }
}
