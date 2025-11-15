namespace GameService.Application;

public class LobbyClosedHandler(IGameCollection gameCollection)
    : MediatR.INotificationHandler<GameDomainEventNotification<Domain.GameEvents.LobbyClosed>>
{
    public Task Handle(
        GameDomainEventNotification<Domain.GameEvents.LobbyClosed> notification,
        CancellationToken cancellationToken
    )
    {
        var game = gameCollection.Get(notification.GameId);

        game.Execute(new Domain.GameCommands.ShuffleCardsInTalon());

        foreach (var player in notification.NewState.Players)
        {
            game.Execute(new Domain.GameCommands.TopUpHand(player.Id));
        }

        game.Execute(new Domain.GameCommands.ChooseCommonTrumpSuit());

        game.Execute(new Domain.GameCommands.StartFirstRound());

        return Task.CompletedTask;
    }
}
