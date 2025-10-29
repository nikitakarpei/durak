namespace GameService.Application;

public interface IGameAccessTokenIssuer
{
    public Task<string> IssueAsync(string gameId, CancellationToken cancellationToken);
}
