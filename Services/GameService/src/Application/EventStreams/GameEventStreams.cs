namespace GameService.Application;

public static class GameEventStreams
{
    public static string DomainEvents(string gameId) => $"game-domain-events/{gameId}";
}
