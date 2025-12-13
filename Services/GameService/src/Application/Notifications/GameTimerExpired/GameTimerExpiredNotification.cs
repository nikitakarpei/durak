namespace GameService.Application;

public record GameTimerExpiredNotification(string GameId, string TimerType, int TimerGeneration)
    : MediatR.INotification;
